using System.Security.Claims;
using System.Text.Encodings.Web;
using Lacos.Application.Interfaces;
using Lacos.Infrastructure.Persistence.Context;
using Lacos.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API completa em memória (pipeline HTTP real: filtros, middlewares, serviços, EF Core e interceptadores).
///
/// Banco de dados: EF Core InMemory, isolado por classe de teste (nenhum schema é criado no Supabase).
///
/// Autenticação: o JWT do Auth0 é substituído por um esquema de teste que lê os cabeçalhos X-Test-*.
/// </summary>
public sealed class LacosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"lacos_test_{Guid.NewGuid():N}";
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"lacos-storage-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Supabase", "Host=unused");
        builder.UseSetting("Encryption:DataKey", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()));
        builder.UseSetting("Encryption:BlindIndexKey", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)(i * 3)).ToArray()));
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.UseSetting("Integrations:Storage:Provider", "Local");
        builder.UseSetting("Integrations:Storage:LocalPath", _storagePath);
        builder.UseSetting("Auth0:Domain", "test.local");
        builder.UseSetting("Auth0:Audience", "test");
        builder.UseSetting("RateLimiting:PermitPerMinute", "10000");

        builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Critical));
        builder.ConfigureTestServices(services =>
        {
            // Substitui o provedor de banco.
            services.RemoveAll<DbContextOptions<LacosDbContext>>();
            services.AddDbContext<LacosDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_databaseName)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(sp.GetRequiredService<TimestampInterceptor>(), sp.GetRequiredService<AuditTrailInterceptor>());
            });
            services.AddScoped<IPublicCodeGenerator, InMemoryPublicCodeGenerator>();

            // Substitui a autenticação JWT pelo esquema de teste.
            services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    /// <summary>Inicializa o banco InMemory dos testes (o provedor InMemory não possui schema real).</summary>
    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LacosDbContext>().Database.EnsureCreatedAsync();
    }

    /// <summary>Cliente HTTP autenticado como o usuário informado.</summary>
    public HttpClient CreateClientFor(string sub, string email, bool emailVerified = true, string? name = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailVerifiedHeader, emailVerified ? "true" : "false");
        client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, name ?? email);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_storagePath)) Directory.Delete(_storagePath, recursive: true);
        base.Dispose(disposing);
    }
}

/// <summary>Gerador de códigos públicos para o provedor InMemory (que não suporta sequências SQL).</summary>
public sealed class InMemoryPublicCodeGenerator : IPublicCodeGenerator
{
    private static int _user;
    private static int _patient;
    public Task<string> NextUserCodeAsync(CancellationToken ct = default) => Task.FromResult($"U{Interlocked.Increment(ref _user):D6}");
    public Task<string> NextPatientCodeAsync(CancellationToken ct = default) => Task.FromResult($"P{Interlocked.Increment(ref _patient):D6}");
}

/// <summary>Esquema de autenticação de teste: monta as claims a partir de cabeçalhos.</summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string SubHeader = "X-Test-Sub";
    public const string EmailHeader = "X-Test-Email";
    public const string EmailVerifiedHeader = "X-Test-EmailVerified";
    public const string NameHeader = "X-Test-Name";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sub = Request.Headers[SubHeader].ToString();
        if (string.IsNullOrEmpty(sub)) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new[]
        {
            new Claim("sub", sub),
            new Claim("email", Request.Headers[EmailHeader].ToString()),
            new Claim("email_verified", Request.Headers[EmailVerifiedHeader].ToString()),
            new Claim("name", Request.Headers[NameHeader].ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName, "sub", null);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
    }
}
