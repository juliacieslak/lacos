using Lacos.Api.Extensions;
using Lacos.Api.Middlewares;
using Lacos.Application;
using Lacos.Infrastructure;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

// -------------------------------------------------------------------------------------------------
// Laços API — ponto de entrada.
// Ordem do pipeline (importa para segurança):
//   cabeçalhos encaminhados → correlação → exceções → cabeçalhos de segurança → HTTPS/HSTS → CORS
//   → autenticação → timeout de sessão → limitação de taxa → autorização → controllers.
// -------------------------------------------------------------------------------------------------

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging estruturado (JSON compacto) com contexto de correlação. Dados sensíveis nunca são registrados.
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Lacos.Api");
        // No ambiente de testes automatizados os logs são suprimidos para manter a saída legível.
        if (!context.HostingEnvironment.IsEnvironment("Testing"))
            configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
    });

    // Limites do servidor: o maior payload aceito é o upload de anexos (10 MB + margem do multipart).
    builder.WebHost.ConfigureKestrel(k =>
    {
        k.Limits.MaxRequestBodySize = 11 * 1024 * 1024;
        k.AddServerHeader = false;
    });

    builder.Services.Configure<SessionSecurityOptions>(builder.Configuration.GetSection(SessionSecurityOptions.SectionName));
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiControllers();
    builder.Services.AddAuth0Authentication(builder.Configuration);
    builder.Services.AddApiRateLimiting(builder.Configuration);
    builder.Services.AddFrontendCors(builder.Configuration);
    builder.Services.AddApiDocumentation();
    builder.Services.AddApiHealthChecks();
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        // Necessário atrás de proxy/load balancer para obter o IP real do cliente (RNF04) e o esquema HTTPS.
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
    builder.Services.AddHsts(o =>
    {
        o.MaxAge = TimeSpan.FromDays(365);
        o.IncludeSubDomains = true;
    });

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(o => o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0} ms");
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Laços API v1"));
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseMiddleware<SessionActivityMiddleware>();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();

    // Aplica migrations automaticamente somente quando habilitado (recomendado apenas em desenvolvimento).
    if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LacosDbContext>().Database.MigrateAsync();
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A aplicação foi encerrada inesperadamente");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program;
