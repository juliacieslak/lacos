using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Lacos.Infrastructure.Integrations.ExternalApis.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Nominatim;

public sealed class NominatimOptions
{
    public const string SectionName = "Integrations:Nominatim";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";
    /// <summary>
    /// Política de uso do Nominatim: é obrigatório um User-Agent que identifique a aplicação, com contato.
    /// </summary>
    public string UserAgent { get; set; } = "Lacos/1.0 (contato@lacos.app)";
    /// <summary>Intervalo mínimo entre chamadas (política pública: no máximo 1 requisição por segundo).</summary>
    public int MinIntervalMilliseconds { get; set; } = 1100;
    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Geocodificação pelo Nominatim (OpenStreetMap) — mesmo provedor usado no protótipo ConsultaCepApp.
///
/// Algoritmo (do mais preciso para o menos preciso), retornando a primeira coordenada encontrada:
/// 1. Logradouro + cidade + UF (STREET) — quando o CEP possui logradouro.
/// 2. CEP (POSTAL_CODE).
/// 3. Cidade + UF (CITY) — centro aproximado da cidade.
/// Resultados ficam em cache por 24 h e as chamadas são espaçadas para respeitar o limite de 1 req/s.
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private static readonly SemaphoreSlim Throttle = new(1, 1);
    private static DateTimeOffset _lastCall = DateTimeOffset.MinValue;

    private readonly HttpClient _http;
    private readonly CachedRequestCoalescer _cache;
    private readonly NominatimOptions _options;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(HttpClient http, CachedRequestCoalescer cache, IOptions<NominatimOptions> options,
        ILogger<NominatimGeocodingService> logger)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public Task<GeoPoint?> GeocodeAsync(AddressLookupResult address, CancellationToken ct = default) =>
        _cache.GetOrAddAsync($"geocode:{address.Cep}", TimeSpan.FromHours(24), () => GeocodeCoreAsync(address, ct));

    private async Task<GeoPoint?> GeocodeCoreAsync(AddressLookupResult address, CancellationToken ct)
    {
        var cep = address.Cep.Length == 8 ? $"{address.Cep[..5]}-{address.Cep[5..]}" : address.Cep;
        var attempts = new List<(string Precision, Dictionary<string, string> Query)>();
        if (!string.IsNullOrWhiteSpace(address.Street))
            attempts.Add(("STREET", new() { ["street"] = address.Street!, ["city"] = address.City, ["state"] = address.State }));
        attempts.Add(("POSTAL_CODE", new() { ["postalcode"] = cep }));
        attempts.Add(("CITY", new() { ["city"] = address.City, ["state"] = address.State }));

        foreach (var (precision, query) in attempts)
        {
            var result = await SearchAsync(query, ct);
            if (result is not null) return result with { Precision = precision };
        }
        _logger.LogWarning("Nominatim não encontrou coordenadas para o CEP {Cep}", address.Cep);
        return null;
    }

    private async Task<GeoPoint?> SearchAsync(Dictionary<string, string> query, CancellationToken ct)
    {
        query["country"] = "Brasil";
        query["countrycodes"] = "br";
        query["format"] = "jsonv2";
        query["limit"] = "1";
        var url = "search?" + string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        await Throttle.WaitAsync(ct);
        try
        {
            var wait = _lastCall.AddMilliseconds(_options.MinIntervalMilliseconds) - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            _lastCall = DateTimeOffset.UtcNow;

            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException("Geocodificação (Nominatim)", $"resposta {(int)response.StatusCode}.");
            var results = await response.Content.ReadFromJsonAsync<List<NominatimResult>>(cancellationToken: ct);
            var first = results?.FirstOrDefault();
            if (first is null
                || !double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return null;
            return new GeoPoint(lat, lon, string.Empty);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException("Geocodificação (Nominatim)", ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ExternalServiceException("Geocodificação (Nominatim)", "tempo de resposta excedido.");
        }
        finally
        {
            Throttle.Release();
        }
    }

    private sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string? Lat,
        [property: JsonPropertyName("lon")] string? Lon,
        [property: JsonPropertyName("display_name")] string? DisplayName);
}
