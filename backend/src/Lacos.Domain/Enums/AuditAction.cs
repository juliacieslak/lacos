namespace Lacos.Domain.Enums;

/// <summary>
/// Ações registradas em audit_logs.action. Valores do modelo: CREATE, UPDATE, DELETE, STATUS_CHANGE, PROFILE_COMPLETED.
/// Acrescentados: VIEW (RNF04 exige registro de visualizações) e ERASURE (exclusão definitiva - LGPD).
/// </summary>
public enum AuditAction
{
    Create,
    Update,
    Delete,
    StatusChange,
    ProfileCompleted,
    View,
    Erasure
}
