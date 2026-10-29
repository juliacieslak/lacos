using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Lacos.Infrastructure.Integrations.ExternalApis.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Infrastructure.Integrations.ExternalApis.Overpass;

public sealed class OverpassOptions
{
    public const string SectionName = "Integrations:Overpass";
    /// <summary>Instâncias públicas da Overpass API; em caso de falha, a próxima é tentada (failover).</summary>
    public string[] Endpoints { get; set; } =
    {
        "https://overpass-api.de/api/interpreter",
        "https://overpass.kumi.systems/api/interpreter"
    };
    /// <summary>Tempo máximo de processamento solicitado ao servidor Overpass (segundos).</summary>
    public int QueryTimeoutSeconds { get; set; } = 20;
    /// <summary>Tempo de vida do cache por região/raio (minutos).</summary>
    public int CacheMinutes { get; set; } = 10;
    public string UserAgent { get; set; } = "Lacos/1.0 (contato@lacos.app)";
}

/// <summary>
/// Busca de farmácias no OpenStreetMap via Overpass API — mesmo provedor do protótipo ConsultaCepApp, com correções:
/// - Consulta nós, vias e relações ("nwr") marcados como amenity=pharmacy OU healthcare=pharmacy
///   (o protótipo considerava apenas um tipo de marcação e perdia estabelecimentos).
/// - "out center" garante coordenadas também para prédios mapeados como área.
/// - Extrai telefone, número, bairro, cidade, UF e CEP quando cadastrados.
/// - Timeout, failover entre instâncias, cache e coalescência de requisições idênticas.
/// </summary>
public sealed class OverpassNearbyPlacesProvider : INearbyPlacesProvider
{
    private readonly HttpClient _http;
    private readonly CachedRequestCoalescer _cache;
    private readonly OverpassOptions _options;
    private readonly ILogger<OverpassNearbyPlacesProvider> _logger;

    public OverpassNearbyPlacesProvider(HttpClient http, CachedRequestCoalescer cache, IOptions<OverpassOptions> options,
        ILogger<OverpassNearbyPlacesProvider> logger)
    {
        _http = http;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public string SourceName => "OpenStreetMap (Overpass API)";

    public async Task<IReadOnlyList<NearbyPlace>> SearchPharmaciesAsync(GeoPoint center, int radiusMeters, CancellationToken ct = default)
    {
        // Arredonda a coordenada (~11 m) para que buscas praticamente iguais reaproveitem o cache.
        var lat = Math.Round(center.Latitude, 4).ToString(CultureInfo.InvariantCulture);
        var lon = Math.Round(center.Longitude, 4).ToString(CultureInfo.InvariantCulture);
        var result = await _cache.GetOrAddAsync($"pharmacies:{lat}:{lon}:{radiusMeters}", TimeSpan.FromMinutes(_options.CacheMinutes),
            () => QueryAsync(lat, lon, radiusMeters, ct));
        return result ?? (IReadOnlyList<NearbyPlace>)Array.Empty<NearbyPlace>();
    }

    private async Task<IReadOnlyList<NearbyPlace>?> QueryAsync(string lat, string lon, int radius, CancellationToken ct)
    {
        var query = $"""
            [out:json][timeout:{_options.QueryTimeoutSeconds}];
            (
              nwr["amenity"="pharmacy"](around:{radius},{lat},{lon});
              nwr["healthcare"="pharmacy"](around:{radius},{lat},{lon});
            );
            out center tags;
            """;

        Exception? lastError = null;
        foreach (var endpoint in _options.Endpoints)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = query })
                };
                request.Headers.UserAgent.ParseAdd(_options.UserAgent);
                using var response = await _http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = new HttpRequestException($"Overpass {endpoint} respondeu {(int)response.StatusCode}");
                    continue;
                }
                var body = await response.Content.ReadFromJsonAsync<OverpassResponse>(cancellationToken: ct);
                return (body?.Elements ?? new List<OverpassElement>()).Select(ToPlace).Where(p => p is not null).Select(p => p!).ToList();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                lastError = ex;
                _logger.LogWarning("Falha na instância Overpass {Endpoint}: {Message}", endpoint, ex.Message);
            }
        }
        throw new ExternalServiceException("Busca de farmácias (OpenStreetMap)",
            "serviço indisponível no momento. Tente novamente em instantes.");
    }

    private static NearbyPlace? ToPlace(OverpassElement e)
    {
        var latitude = e.Lat ?? e.Center?.Lat;
        var longitude = e.Lon ?? e.Center?.Lon;
        if (latitude is null || longitude is null) return null;
        string? Tag(params string[] keys) => keys.Select(k => e.Tags?.GetValueOrDefault(k)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        return new NearbyPlace($"osm:{e.Type}/{e.Id}", Tag("name", "brand"), Tag("addr:street"), Tag("addr:housenumber"),
            Tag("addr:suburb", "addr:neighbourhood", "addr:district"), Tag("addr:city"), Tag("addr:state"), Tag("addr:postcode"),
            Tag("phone", "contact:phone", "contact:mobile"), latitude.Value, longitude.Value);
    }

    private sealed class OverpassResponse
    {
        [JsonPropertyName("elements")] public List<OverpassElement>? Elements { get; set; }
    }

    private sealed class OverpassElement
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "node";
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("lat")] public double? Lat { get; set; }
        [JsonPropertyName("lon")] public double? Lon { get; set; }
        [JsonPropertyName("center")] public OverpassCenter? Center { get; set; }
        [JsonPropertyName("tags")] public Dictionary<string, string>? Tags { get; set; }
    }

    private sealed class OverpassCenter
    {
        [JsonPropertyName("lat")] public double Lat { get; set; }
        [JsonPropertyName("lon")] public double Lon { get; set; }
    }
}
