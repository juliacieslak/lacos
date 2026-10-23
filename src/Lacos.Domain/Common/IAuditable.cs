namespace Lacos.Domain.Common;

/// <summary>
/// Marcador para entidades cujas inclusões, alterações e exclusões devem ser registradas
/// automaticamente na trilha de auditoria (RF06, RF10 e RNF04).
/// O interceptador de auditoria da infraestrutura detecta as entidades que implementam
/// esta interface e grava um registro em <c>audit_logs</c> na mesma transação.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Nome lógico da entidade gravado em <c>audit_logs.entity_type</c>
    /// (ex.: USER, PATIENT, HEALTH_THRESHOLD, PATIENT_EVENT, MEDICATION).
    /// </summary>
    string AuditEntityType { get; }

    /// <summary>
    /// Paciente relacionado à ação, quando aplicável (audit_logs.patient_id).
    /// </summary>
    Guid? AuditPatientId { get; }
}
