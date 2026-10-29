namespace Lacos.Application.DTOs;

/// <summary>Farmácia encontrada próximo ao CEP. Campos nulos = informação não disponibilizada pelo provedor.</summary>
public sealed record PharmacyResponse(
    string Id, string Name, bool HasRegisteredName, string? Street, string? HouseNumber, string? Neighborhood,
    string? City, string? State, string? Postcode, string? Phone, double Latitude, double Longitude,
    int DistanceMeters, string GoogleMapsUrl);

/// <summary>Ponto de origem usado na busca (coordenada aproximada do CEP).</summary>
public sealed record SearchOriginResponse(double Latitude, double Longitude, string Precision);

/// <summary>Resultado da busca de farmácias por CEP.</summary>
public sealed record PharmacySearchResponse(
    string Cep, string? Street, string? Neighborhood, string City, string State, SearchOriginResponse Origin,
    int RadiusMeters, string Source, IReadOnlyList<PharmacyResponse> Pharmacies);
