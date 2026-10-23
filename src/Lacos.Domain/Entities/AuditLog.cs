using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Registro da trilha de auditoria (tabela <c>audit_logs</c>) — RF06, RF10 e RNF04.
///
/// Regras:
/// - Registro IMUTÁVEL (RNF04): a entidade não possui métodos de alteração, o repositório só permite inclusão
///   e o banco possui trigger que bloqueia UPDATE/DELETE (exceto a redação de valores exigida pela LGPD).
/// - RNF04 exige data, hora e IP: foram acrescentadas as colunas <see cref="IpAddress"/> e <see cref="UserAgent"/>.
/// - Visualizações de dados médicos também são registradas (ação VIEW).
/// - Valores de campos sensíveis são gravados criptografados dentro de old_values/new_values.
/// </summary>
public class AuditLog
{
    private AuditLog() { }

    public AuditLog(Guid? userId, Guid? patientId, string entityType, Guid? entityId, AuditAction action,
        string? oldValues, string? newValues, string? reason, string? ipAddress, string? userAgent, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        PatientId = patientId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        Reason = reason;
        IpAddress = ipAddress;
        UserAgent = userAgent is { Length: > 512 } ? userAgent[..512] : userAgent;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    /// <summary>Usuário responsável; nulo apenas para ações do próprio sistema (ex.: rotinas automáticas).</summary>
    public Guid? UserId { get; private set; }
    public Guid? PatientId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    /// <summary>JSON (jsonb) com os valores anteriores.</summary>
    public string? OldValues { get; private set; }
    /// <summary>JSON (jsonb) com os novos valores.</summary>
    public string? NewValues { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User? User { get; private set; }
}
