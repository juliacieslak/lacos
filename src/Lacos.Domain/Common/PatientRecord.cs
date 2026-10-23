namespace Lacos.Domain.Common;

/// <summary>
/// Base para todo registro clínico/de cuidado vinculado a um paciente
/// (medicamentos, métricas, sintomas, diário, condições, cirurgias, alergias, documentos etc.).
///
/// Regras de negócio comuns implementadas aqui:
/// - Todo registro pertence a exatamente um paciente (RF19: "mantendo-os vinculados ao respectivo registro do paciente").
/// - O usuário que criou e o último que alterou são registrados automaticamente (RF10).
/// - A exclusão é lógica (soft delete) para preservar o histórico (RF06).
/// </summary>
public abstract class PatientRecord : BaseEntity, IAuditable, ISoftDeletable
{
    protected PatientRecord() { }

    protected PatientRecord(Guid patientId, Guid createdByUserId)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("Paciente obrigatório.", nameof(patientId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Usuário responsável obrigatório.", nameof(createdByUserId));
        PatientId = patientId;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>Paciente ao qual o registro pertence.</summary>
    public Guid PatientId { get; protected set; }

    /// <summary>Usuário que cadastrou o registro (identificado automaticamente - RF10).</summary>
    public Guid CreatedByUserId { get; protected set; }

    /// <summary>Último usuário que alterou o registro (RF10).</summary>
    public Guid? UpdatedByUserId { get; protected set; }

    public DateTimeOffset? DeletedAt { get; protected set; }
    public Guid? DeletedByUserId { get; protected set; }

    public abstract string AuditEntityType { get; }
    public Guid? AuditPatientId => PatientId;

    /// <summary>
    /// Registra o usuário responsável pela alteração. Deve ser chamado por toda operação de edição.
    /// </summary>
    public void TouchBy(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Usuário responsável obrigatório.", nameof(userId));
        UpdatedByUserId = userId;
    }

    /// <summary>
    /// Exclusão lógica: o registro deixa de aparecer nas consultas (filtro global),
    /// mas permanece no banco para preservar a trilha de auditoria.
    /// </summary>
    public void MarkAsDeleted(Guid userId, DateTimeOffset when)
    {
        if (DeletedAt is not null) return; // Idempotente: excluir duas vezes não altera o responsável original.
        DeletedAt = when;
        DeletedByUserId = userId;
        UpdatedByUserId = userId;
    }
}
