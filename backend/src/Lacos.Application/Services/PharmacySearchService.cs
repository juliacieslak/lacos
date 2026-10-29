using System.Globalization;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Domain.Exceptions;
using Lacos.Domain.ValueObjects;

namespace Lacos.Application.Services;

/// <summary>Busca de farmácias próximas a partir de um CEP brasileiro.</summary>
public interface IPharmacySearchService
{
    Task<PharmacySearchResponse> SearchByCepAsync(string cep, int? radiusMeters, CancellationToken ct = default);
}

/// <summary>
/// Orquestra as três integrações necessárias (nenhuma delas, isoladamente, oferece tudo):
/// 1. ViaCEP → valida a existência do CEP e obtém logradouro, bairro, cidade e UF (não fornece coordenadas).
/// 2. Geocodificação (Nominatim) → coordenadas aproximadas do endereço do CEP.
/// 3. Provedor de locais (Overpass/OpenStreetMap) → farmácias dentro do raio.
/// Em seguida remove duplicidades, calcula a distância (fórmula de Haversine) e ordena da mais próxima para a mais distante.
/// Nenhum dado é inventado: campos não informados pelo provedor retornam nulos.
/// </summary>
public sealed class PharmacySearchService : IPharmacySearchService
{
    public const int DefaultRadiusMeters = 3000;
    public const int MinRadiusMeters = 500;
    public const int MaxRadiusMeters = 10000;
    public const int MaxResults = 50;

    /// <summary>Distância abaixo da qual dois registros com o mesmo nome são considerados o mesmo estabelecimento.</summary>
    private const double DuplicateDistanceMeters = 60;

    private readonly IAddressLookupService _address;
    private readonly IGeocodingService _geocoding;
    private readonly INearbyPlacesProvider _places;

    public PharmacySearchService(IAddressLookupService address, IGeocodingService geocoding, INearbyPlacesProvider places)
    {
        _address = address;
        _geocoding = geocoding;
        _places = places;
    }

    public async Task<PharmacySearchResponse> SearchByCepAsync(string cep, int? radiusMeters, CancellationToken ct = default)
    {
        // 1. Validação local do CEP (formato) antes de qualquer chamada externa.
        if (!Cep.TryCreate(cep, out var parsed))
            throw new ValidationFailedException(nameof(cep), "CEP inválido. Informe 8 dígitos (ex.: 80010-000).");
        var radius = radiusMeters ?? DefaultRadiusMeters;
        if (radius is < MinRadiusMeters or > MaxRadiusMeters)
            throw new ValidationFailedException("radius", $"O raio deve estar entre {MinRadiusMeters} e {MaxRadiusMeters} metros.");

        // 2. CEP existente?
        var address = await _address.LookupAsync(parsed!.Value, ct)
                      ?? throw new NotFoundException("CEP", parsed.Formatted);

        // 3. Coordenadas aproximadas.
        var origin = await _geocoding.GeocodeAsync(address, ct)
                     ?? throw new BusinessRuleException("GEOCODING_FAILED",
                         "Não foi possível localizar as coordenadas deste CEP. Tente um CEP próximo.");

        // 4. Farmácias no raio, sem duplicidades, ordenadas por distância.
        var places = await _places.SearchPharmaciesAsync(origin, radius, ct);
        var pharmacies = Deduplicate(places)
            .Select(p => ToResponse(p, Haversine(origin.Latitude, origin.Longitude, p.Latitude, p.Longitude)))
            .Where(p => p.DistanceMeters <= radius)
            .OrderBy(p => p.DistanceMeters).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxResults)
            .ToList();

        return new PharmacySearchResponse(parsed.Formatted, address.Street, address.Neighborhood, address.City, address.State,
            new SearchOriginResponse(origin.Latitude, origin.Longitude, origin.Precision), radius, _places.SourceName, pharmacies);
    }

    /// <summary>
    /// Remove duplicidades em dois níveis:
    /// - mesmo identificador do provedor (ex.: retornado por mais de um filtro);
    /// - mesmo nome normalizado a menos de 60 m (ex.: o ponto e o contorno do prédio mapeados separadamente).
    /// Entre duplicados, mantém o registro com mais informações de endereço/telefone.
    /// </summary>
    public static IReadOnlyList<NearbyPlace> Deduplicate(IEnumerable<NearbyPlace> places)
    {
        var unique = new List<NearbyPlace>();
        foreach (var place in places.GroupBy(p => p.ExternalId).Select(g => g.OrderByDescending(Completeness).First()))
        {
            var key = Normalize(place.Name);
            var index = key.Length == 0 ? -1 : unique.FindIndex(u => Normalize(u.Name) == key
                && Haversine(u.Latitude, u.Longitude, place.Latitude, place.Longitude) <= DuplicateDistanceMeters);
            if (index < 0) unique.Add(place);
            else if (Completeness(place) > Completeness(unique[index])) unique[index] = place;
        }
        return unique;
    }

    /// <summary>Distância em metros entre duas coordenadas (fórmula de Haversine, raio médio da Terra 6.371 km).</summary>
    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000;
        static double Rad(double deg) => deg * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static PharmacyResponse ToResponse(NearbyPlace p, double distance)
    {
        var lat = p.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lon = p.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
        var hasName = !string.IsNullOrWhiteSpace(p.Name);
        return new PharmacyResponse(p.ExternalId, hasName ? p.Name!.Trim() : "Farmácia (nome não cadastrado)", hasName,
            p.Street, p.HouseNumber, p.Neighborhood, p.City, p.State, p.Postcode, p.Phone, p.Latitude, p.Longitude,
            (int)Math.Round(distance), $"https://www.google.com/maps/search/?api=1&query={lat},{lon}");
    }

    private static int Completeness(NearbyPlace p) =>
        new[] { p.Name, p.Street, p.HouseNumber, p.Neighborhood, p.City, p.Phone }.Count(v => !string.IsNullOrWhiteSpace(v));

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
