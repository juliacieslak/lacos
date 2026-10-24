using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Lacos.Api.Filters;
using Lacos.Api.Middlewares;
using Lacos.Api.Security;
using Lacos.Application.Interfaces;
using Lacos.Infrastructure.Integrations.ExternalApis.Auth0;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Lacos.Api.Extensions;

/// <summary>Configuração da camada de apresentação (API), separada do Program.cs para legibilidade.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Controllers, filtros globais e serialização JSON (enums como texto UPPER_SNAKE_CASE, iguais ao banco).</summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ValidationFilter>();
        services.AddScoped<ActiveUserFilter>();

        services.AddControllers(options =>
            {
                options.Filters.AddService<ActiveUserFilter>(order: 0);
                options.Filters.AddService<ValidationFilter>(order: 1);
                // Toda rota exige usuário autenticado, salvo [AllowAnonymous] explícito.
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
                options.ModelBinderProviders.Insert(0, new Lacos.Api.ModelBinding.UpperSnakeEnumModelBinderProvider());
            })
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            })
            .ConfigureApiBehaviorOptions(o =>
            {
                // Erros de binding/modelo seguem o mesmo formato ProblemDetails com "code".
                o.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = 400,
                        Title = "Dados inválidos",
                        Detail = "A requisição contém campos inválidos ou mal formatados."
                    };
                    problem.Extensions["code"] = "VALIDATION_FAILED";
                    problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
                };
            });
        return services;
    }

    /// <summary>
    /// Autenticação JWT emitida pelo Auth0 (RF02): valida emissor, audiência, assinatura (JWKS) e expiração.
    /// MapInboundClaims=false preserva os nomes originais das claims ("sub", "email").
    /// </summary>
    public static IServiceCollection AddAuth0Authentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth0 = configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>() ?? new Auth0Options();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{auth0.Domain}/";
                options.Audience = auth0.Audience;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub"
                };
            });
        services.AddAuthorization(o =>
        {
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        });
        return services;
    }

    /// <summary>
    /// Limitação de taxa (proteção contra abuso e força bruta):
    /// - Global: 120 requisições/minuto por usuário (ou IP, se anônimo).
    /// - "uploads": 10 envios/minuto por usuário.
    /// - "sensitive" (exclusões definitivas): 5/hora por usuário.
    /// - "location-search" (busca de farmácias): 20/minuto por usuário.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var perMinute = configuration.GetValue("RateLimiting:PermitPerMinute", 120);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = perMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
            options.AddPolicy("uploads", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ =>
                new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
            // Buscas de localização consomem APIs públicas com limite de uso: 20 por minuto por usuário.
            options.AddPolicy("location-search", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ =>
                new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
            options.AddPolicy("sensitive", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ =>
                new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));
            options.OnRejected = async (context, ct) =>
            {
                var problem = new ProblemDetails { Status = 429, Title = "Muitas requisições", Detail = "Aguarde alguns instantes e tente novamente." };
                problem.Extensions["code"] = "RATE_LIMITED";
                await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
            };
        });
        return services;

        static string PartitionKey(HttpContext ctx) =>
            ctx.User.FindFirstValue("sub") ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    }

    /// <summary>CORS restrito às origens do front-end configuradas (nunca "*" com credenciais).</summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(o => o.AddPolicy("Frontend", p => p
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Authorization", "Content-Type", CorrelationIdMiddleware.HeaderName)
            .WithExposedHeaders(CorrelationIdMiddleware.HeaderName, "Content-Disposition")
            .SetPreflightMaxAge(TimeSpan.FromHours(1))));
        return services;
    }

    /// <summary>Documentação OpenAPI/Swagger com suporte a token Bearer (somente em desenvolvimento).</summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Laços API",
                Version = "v1",
                Description = "Back-end do sistema Laços — gestão de cuidados de pacientes por familiares, cuidadores e profissionais da saúde."
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token emitido pelo Auth0."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                }
            });
            c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
            c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
            c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time", Example = new Microsoft.OpenApi.Any.OpenApiString("08:00:00") });
        });
        return services;
    }

    /// <summary>Health check (RNF08): verifica a conexão com o banco; usado por monitoramento/orquestrador.</summary>
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<LacosDbContext>("database");
        return services;
    }
}
