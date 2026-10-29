namespace Lacos.Application.Interfaces.Integrations;

// ---------------------------------------------------------------------------------------------
// PONTOS DE EXTENSÃO PARA INTEGRAÇÕES EXTERNAS
// A camada de aplicação depende somente destas interfaces. As implementações concretas ficam em
// Infrastructure/Integrations/ExternalApis e podem ser trocadas por configuração (appsettings) sem
// alterar regras de negócio (princípios Open/Closed e Dependency Inversion).
// ---------------------------------------------------------------------------------------------

/// <summary>Armazenamento de arquivos (Supabase Storage em produção; disco local em desenvolvimento).</summary>
public interface IFileStorage
{
    Task UploadAsync(string path, byte[] content, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(IEnumerable<string> paths, CancellationToken ct = default);
}

/// <summary>Consulta de endereço por CEP (ex.: ViaCEP) — apoio ao cadastro de RF01/RF03.</summary>
public interface IAddressLookupService
{
    Task<AddressLookupResult?> LookupAsync(string cep, CancellationToken ct = default);
}

public sealed record AddressLookupResult(string Cep, string? Street, string? Neighborhood, string City, string State);

/// <summary>Consulta de códigos CID (ex.: API ICD da OMS) — apoio ao RF14.</summary>
public interface ICidLookupService
{
    Task<IReadOnlyList<CidLookupResult>> SearchAsync(string term, CancellationToken ct = default);
}

public sealed record CidLookupResult(string Code, string Title);

/// <summary>Operações administrativas no provedor de identidade (Auth0 Management API).</summary>
public interface IIdentityProviderService
{
    /// <summary>Remove a conta no provedor de identidade (exclusão definitiva - LGPD).</summary>
    Task DeleteUserAsync(string auth0UserId, CancellationToken ct = default);
}

/// <summary>Envio de e-mails transacionais (convites, alertas).</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Envio de notificações push (ex.: Firebase Cloud Messaging / OneSignal).</summary>
public interface IPushNotificationSender
{
    Task SendAsync(Guid userId, string title, string message, IDictionary<string, string>? data, CancellationToken ct = default);
}

/// <summary>Sinaliza o worker de notificações para processar a fila imediatamente (RNF11: até 1 minuto).</summary>
public interface INotificationDispatchSignal
{
    void Signal();
}

/// <summary>Coordenada geográfica com a precisão obtida na geocodificação.</summary>
/// <param name="Precision">STREET (logradouro), POSTAL_CODE (CEP) ou CITY (centro da cidade).</param>
public sealed record GeoPoint(double Latitude, double Longitude, string Precision);

/// <summary>
/// Geocodificação: converte o endereço de um CEP em coordenadas aproximadas.
/// A API de CEP (ViaCEP) não fornece coordenadas, por isso este serviço complementar é necessário.
/// Implementação atual: Nominatim (OpenStreetMap). Pode ser trocada (ex.: Google Geocoding) sem alterar a regra de negócio.
/// </summary>
public interface IGeocodingService
{
    Task<GeoPoint?> GeocodeAsync(AddressLookupResult address, CancellationToken ct = default);
}

/// <summary>Estabelecimento retornado pelo provedor, apenas com os dados que o provedor realmente informou.</summary>
public sealed record NearbyPlace(
    string ExternalId, string? Name, string? Street, string? HouseNumber, string? Neighborhood, string? City,
    string? State, string? Postcode, string? Phone, double Latitude, double Longitude);

/// <summary>
/// Busca de estabelecimentos próximos a uma coordenada.
/// Implementação atual: Overpass API (OpenStreetMap). Pode ser trocada (ex.: Google Places) sem alterar a regra de negócio.
/// </summary>
public interface INearbyPlacesProvider
{
    /// <summary>Nome do provedor exibido como fonte dos dados.</summary>
    string SourceName { get; }
    Task<IReadOnlyList<NearbyPlace>> SearchPharmaciesAsync(GeoPoint center, int radiusMeters, CancellationToken ct = default);
}
