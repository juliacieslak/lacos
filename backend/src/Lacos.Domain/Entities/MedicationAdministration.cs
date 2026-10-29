using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Registro de administração de uma dose (tabela <c>medication_administrations</c>).
/// Origem: US02 ("registrar os medicamentos ministrados"), validação com cuidador (Sprint 3: "opção simples para
/// marcar medicamentos administrados") e RF12 ("se a medicação não foi administrada").
///
/// Regras:
/// - <see cref="ScheduledFor"/> identifica a ocorrência programada (data + horário); é nulo para doses extras.
/// - Só pode existir um registro por ocorrência programada (índice único medication_id + scheduled_for).
/// - A data/hora e o usuário são associados automaticamente ao salvar (US02).
/// </summary>
public class MedicationAdministration : PatientRecord
{
    private MedicationAdministration() { }

    public MedicationAdministration(Guid patientId, Guid medicationId, Guid recordedByUserId,
        DateTimeOffset? scheduledFor, DateTimeOffset administeredAt, MedicationAdministrationStatus status, string? notes)
        : base(patientId, recordedByUserId)
    {
        MedicationId = medicationId;
        ScheduledFor = scheduledFor;
        AdministeredAt = administeredAt;
        Status = status;
        Notes = notes?.Trim();
    }

    public Guid MedicationId { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset AdministeredAt { get; private set; }
    public MedicationAdministrationStatus Status { get; private set; }
    [SensitiveData] public string? Notes { get; private set; }

    public Medication? Medication { get; private set; }

    public override string AuditEntityType => "MEDICATION_ADMINISTRATION";

    public void Update(DateTimeOffset administeredAt, MedicationAdministrationStatus status, string? notes, Guid userId)
    {
        AdministeredAt = administeredAt;
        Status = status;
        Notes = notes?.Trim();
        TouchBy(userId);
    }
}
