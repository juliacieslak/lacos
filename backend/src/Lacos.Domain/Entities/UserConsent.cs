using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Registro de consentimento LGPD (tabela <c>user_consents</c>) — RNF01:
/// "exigindo aceite dos termos de uso, consentimento para tratamento de dados sensíveis de saúde".
/// Cada aceite guarda a versão do documento, data/hora e IP como prova do consentimento.
/// A revogação é registrada sem apagar o histórico.
/// </summary>
public class UserConsent : BaseEntity, IAuditable
{
    private UserConsent() { }

    public UserConsent(Guid userId, ConsentType consentType, string version, DateTimeOffset acceptedAt, string? ipAddress)
    {
        UserId = userId;
        ConsentType = consentType;
        Version = version;
        AcceptedAt = acceptedAt;
        IpAddress = ipAddress;
    }

    public Guid UserId { get; private set; }
    public ConsentType ConsentType { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public DateTimeOffset AcceptedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public string AuditEntityType => "USER_CONSENT";
    public Guid? AuditPatientId => null;

    public bool IsValidFor(string currentVersion) => RevokedAt is null && Version == currentVersion;

    public void Revoke(DateTimeOffset when) => RevokedAt ??= when;
}
