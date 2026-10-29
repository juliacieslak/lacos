namespace Lacos.Api.Middlewares;

/// <summary>
/// Cabeçalhos HTTP de segurança (OWASP Secure Headers):
/// - Impede MIME sniffing, uso em iframes (clickjacking) e vazamento de referer.
/// - CSP restritiva (a API não serve HTML, exceto a documentação Swagger em desenvolvimento).
/// - Cache-Control no-store: respostas contêm dados de saúde e não devem ficar em cache de navegadores/proxies (LGPD).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                headers["Cache-Control"] = "no-store";
                headers["Pragma"] = "no-cache";
            }
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            return Task.CompletedTask;
        });
        return _next(context);
    }
}
