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
