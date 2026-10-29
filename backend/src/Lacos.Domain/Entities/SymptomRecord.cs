using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Sintomas, queixas e alterações percebidas no estado de saúde (tabela <c>symptom_records</c>) — RF09.
/// Informa descrição, data e horário da ocorrência.
/// </summary>
public class SymptomRecord : PatientRecord
{
    private SymptomRecord() { }

    public SymptomRecord(Guid patientId, Guid recordedByUserId, SymptomType type, string description, DateTimeOffset occurredAt)
        : base(patientId, recordedByUserId)
    {
        Type = type;
        Description = description.Trim();
        OccurredAt = occurredAt;
    }

    public SymptomType Type { get; private set; }
    [SensitiveData] public string Description { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    public override string AuditEntityType => "SYMPTOM_RECORD";

    public void Update(SymptomType type, string description, DateTimeOffset occurredAt, Guid userId)
    {
        Type = type;
        Description = description.Trim();
        OccurredAt = occurredAt;
        TouchBy(userId);
    }
}
