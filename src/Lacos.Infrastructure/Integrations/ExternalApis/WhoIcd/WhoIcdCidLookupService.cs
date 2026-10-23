using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.WhoIcd;

public sealed class WhoIcdOptions
{
    public const string SectionName = "Integrations:WhoIcd";
    public bool Enabled { get; set; }
    public string TokenUrl { get; set; } = "https://icdaccessmanagement.who.int/connect/token";
    public string ApiBaseUrl { get; set; } = "https://id.who.int/";
    public string Release { get; set; } = "2024-01";
    public string Language { get; set; } = "pt";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>
/// Busca de códigos CID na API ICD da Organização Mundial da Saúde (CID-11, linearização MMS) — apoio ao RF14.
/// Autenticação OAuth2 client credentials; o token é reaproveitado (cache) até perto da expiração.
/// </summary>
public sealed partial class WhoIcdCidLookupService : ICidLookupService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly WhoIcdOptions _options;

    public WhoIcdCidLookupService(HttpClient http, IMemoryCache cache, IOptions<WhoIcdOptions> options)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<CidLookupResult>> SearchAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var token = await GetTokenAsync(ct);
            var url = $"{_options.ApiBaseUrl.TrimEnd('/')}/icd/release/11/{_options.Release}/mms/search?q={Uri.EscapeDataString(term)}&flatResults=true";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("API-Version", "v2");
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(_options.Language));
            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: ct);
            return body?.DestinationEntities?
                .Where(e => !string.IsNullOrEmpty(e.TheCode))
                .Select(e => new CidLookupResult(e.TheCode!, StripTags().Replace(e.Title ?? string.Empty, string.Empty)))
                .Take(20).ToList() ?? new List<CidLookupResult>();
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("API ICD (OMS)", ex.Message);
        }
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue("who-icd-token", out string? cached) && cached is not null) return cached;
        using var response = await _http.PostAsync(_options.TokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["scope"] = "icdapi_access",
            ["grant_type"] = "client_credentials"
        }), ct);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                    ?? throw new ExternalServiceException("API ICD (OMS)", "token inválido.");
        _cache.Set("who-icd-token", token.AccessToken, TimeSpan.FromSeconds(Math.Max(60, token.ExpiresIn - 60)));
        return token.AccessToken;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex StripTags();

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record SearchResponse([property: JsonPropertyName("destinationEntities")] List<Entity>? DestinationEntities);

    private sealed record Entity([property: JsonPropertyName("theCode")] string? TheCode, [property: JsonPropertyName("title")] string? Title);
}

/// <summary>Implementação usada quando a integração com a OMS não está configurada.</summary>
public sealed class UnavailableCidLookupService : ICidLookupService
{
    public Task<IReadOnlyList<CidLookupResult>> SearchAsync(string term, CancellationToken ct = default) =>
        throw new ExternalServiceException("Busca de CID", "integração não configurada (Integrations:WhoIcd).");
}
