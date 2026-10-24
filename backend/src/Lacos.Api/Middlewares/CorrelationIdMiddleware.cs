using Serilog.Context;

namespace Lacos.Api.Middlewares;

/// <summary>
/// Atribui um identificador de correlação a cada requisição (cabeçalho X-Correlation-Id) e o inclui em todos os logs
/// estruturados, permitindo rastrear uma operação ponta a ponta (front-end → API → integrações).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        // Aceita apenas identificadores curtos e seguros (evita injeção em logs).
        var correlationId = !string.IsNullOrWhiteSpace(incoming) && incoming.Length <= 64 && incoming.All(c => char.IsLetterOrDigit(c) || c == '-')
            ? incoming
            : context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
