using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Auth0;

/// <summary>
/// Configuração do Auth0: validação de tokens (Domain/Audience) e Management API (M2M) para operações administrativas.
/// </summary>
public sealed class Auth0Options
{
    public const string SectionName = "Auth0";
    /// <summary>Domínio do tenant, ex.: lacos.us.auth0.com.</summary>
    public string Domain { get; set; } = string.Empty;
    /// <summary>Identificador da API registrada no Auth0 (claim "aud").</summary>
    public string Audience { get; set; } = string.Empty;
    /// <summary>Namespace das claims personalizadas adicionadas por uma Auth0 Action (e-mail, nome).</summary>
    public string ClaimsNamespace { get; set; } = "https://lacos.app/";
    public string ManagementClientId { get; set; } = string.Empty;
    public string ManagementClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Operações na Auth0 Management API. Usada na exclusão definitiva da conta (LGPD - RNF01).
/// </summary>
public sealed class Auth0ManagementService : IIdentityProviderService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly Auth0Options _options;
    private readonly ILogger<Auth0ManagementService> _logger;

    public Auth0ManagementService(HttpClient http, IMemoryCache cache, IOptions<Auth0Options> options, ILogger<Auth0ManagementService> logger)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task DeleteUserAsync(string auth0UserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ManagementClientId))
        {
            _logger.LogWarning("Auth0 Management API não configurada; usuário {Auth0UserId} não foi removido do Auth0", auth0UserId);
            return;
        }
        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"https://{_options.Domain}/api/v2/users/{Uri.EscapeDataString(auth0UserId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("Auth0", $"falha ao remover usuário ({(int)response.StatusCode}).");
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("auth0-mgmt-token", out string? cached) && cached is not null) return cached;
        using var response = await _http.PostAsJsonAsync($"https://{_options.Domain}/oauth/token", new
        {
            client_id = _options.ManagementClientId,
            client_secret = _options.ManagementClientSecret,
            audience = $"https://{_options.Domain}/api/v2/",
            grant_type = "client_credentials"
        }, ct);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                    ?? throw new ExternalServiceException("Auth0", "token inválido.");
        _cache.Set("auth0-mgmt-token", token.AccessToken, TimeSpan.FromSeconds(Math.Max(60, token.ExpiresIn - 120)));
        return token.AccessToken;
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
