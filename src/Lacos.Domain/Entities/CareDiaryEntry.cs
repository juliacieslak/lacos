using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Entrada do diário de acompanhamento do paciente (tabela <c>care_diary_entries</c>) — RF04 / UC07.
/// Registra atividades realizadas, alimentação, higiene, sono, medicação, cuidados prestados,
/// queixas e dores ao longo do dia.
/// </summary>
public class CareDiaryEntry : PatientRecord
{
    private CareDiaryEntry() { }

    public CareDiaryEntry(Guid patientId, Guid recordedByUserId, CareDiaryCategory category, string description, DateTimeOffset occurredAt)
        : base(patientId, recordedByUserId)
    {
        Category = category;
        Description = description.Trim();
        OccurredAt = occurredAt;
    }

    public CareDiaryCategory Category { get; private set; }
    [SensitiveData] public string Description { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }

    public override string AuditEntityType => "CARE_DIARY_ENTRY";

    public void Update(CareDiaryCategory category, string description, DateTimeOffset occurredAt, Guid userId)
    {
        Category = category;
        Description = description.Trim();
        OccurredAt = occurredAt;
        TouchBy(userId);
    }
}
