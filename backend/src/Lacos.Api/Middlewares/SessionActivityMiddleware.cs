using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Lacos.Api.Middlewares;

public sealed class SessionSecurityOptions
{
    public const string SectionName = "Session";
    /// <summary>RNF03: desconexão automática após 15 minutos de inatividade.</summary>
    public int InactivityTimeoutMinutes { get; set; } = 15;
}

/// <summary>
/// Timeout de sessão por inatividade (RNF03) e logout com revogação do token.
///
/// Algoritmo:
/// - A "sessão" é identificada pelo hash SHA-256 do access token (o token nunca é armazenado em claro).
/// - A cada requisição autenticada, compara o instante atual com a última atividade registrada no cache distribuído.
/// - Se o intervalo exceder o limite, responde 401 SESSION_EXPIRED: o front-end deve exigir novo login.
///   Caso contrário, atualiza a última atividade (expiração deslizante).
/// - No logout, o hash do token é marcado como revogado até a expiração natural do token.
/// Em produção com várias instâncias, configure um cache distribuído (ex.: Redis) — ver README.
/// Recomenda-se também configurar o "Inactivity timeout" do Auth0 com o mesmo valor.
/// </summary>
public sealed class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;

    public SessionActivityMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache, IOptions<SessionSecurityOptions> options)
    {
        if (context.User.Identity?.IsAuthenticated != true || !TryGetToken(context, out var token))
        {
            await _next(context);
            return;
        }

        var key = SessionKey(token);
        var ct = context.RequestAborted;
        if (await cache.GetStringAsync($"{key}:revoked", ct) is not null)
        {
            await RejectAsync(context, "SESSION_REVOKED", "A sessão foi encerrada. Faça login novamente.");
            return;
        }

        var timeout = TimeSpan.FromMinutes(options.Value.InactivityTimeoutMinutes);
        var now = DateTimeOffset.UtcNow;
        var last = await cache.GetStringAsync(key, ct);
        if (last is not null && long.TryParse(last, out var ticks) && now - new DateTimeOffset(ticks, TimeSpan.Zero) > timeout)
        {
            await RejectAsync(context, "SESSION_EXPIRED", "Sessão expirada por inatividade. Faça login novamente.");
            return;
        }

        await cache.SetStringAsync(key, now.UtcTicks.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        }, ct);
        await _next(context);
    }

    /// <summary>Revoga a sessão do token atual (logout).</summary>
    public static async Task RevokeAsync(HttpContext context, IDistributedCache cache)
    {
        if (!TryGetToken(context, out var token)) return;
        var exp = context.User.FindFirst("exp")?.Value;
        var expiresAt = long.TryParse(exp, out var seconds) ? DateTimeOffset.FromUnixTimeSeconds(seconds) : DateTimeOffset.UtcNow.AddHours(24);
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero) return;
        await cache.SetStringAsync($"{SessionKey(token)}:revoked", "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, context.RequestAborted);
    }

    private static bool TryGetToken(HttpContext context, out string token)
    {
        var header = context.Request.Headers.Authorization.ToString();
        token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
        return token.Length > 0;
    }

    private static string SessionKey(string token) =>
        "session:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static async Task RejectAsync(HttpContext context, string code, string message)
    {
        var problem = new ProblemDetails { Status = 401, Title = "Sessão inválida", Detail = message };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }
}
