using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Compromisso da agenda do paciente (tabela <c>patient_events</c>) — RF18 / WF03.
/// Campos: tipo, título/descrição, data e hora, duração em minutos, local, profissional responsável,
/// status, resumo da ocasião e anotações adicionais.
///
/// Regras de transição de status (SUPOSIÇÃO documentada, a documentação não define a máquina de estados):
/// - SCHEDULED → COMPLETED, CANCELLED ou MISSED.
/// - CANCELLED/MISSED → SCHEDULED (reagendamento).
/// - COMPLETED é final (o resumo pode continuar sendo editado).
/// </summary>
public class PatientEvent : PatientRecord
{
    private PatientEvent() { }

    public PatientEvent(Guid patientId, Guid createdByUserId, PatientEventType eventType, string title,
        DateTimeOffset scheduledAt, int? durationMinutes, string? locationName, string? providerName, string? notes)
        : base(patientId, createdByUserId)
    {
        Status = PatientEventStatus.Scheduled;
        Update(eventType, title, scheduledAt, durationMinutes, locationName, providerName, notes, createdByUserId);
        UpdatedByUserId = null;
    }

    public PatientEventType EventType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? LocationName { get; private set; }
    public string? ProviderName { get; private set; }
    public PatientEventStatus Status { get; private set; }

    /// <summary>Resumo do atendimento/resultado (dado clínico sensível - RNF02).</summary>
    [SensitiveData]
    public string? Summary { get; private set; }

    /// <summary>Anotações adicionais (podem conter dados clínicos - RNF02).</summary>
    [SensitiveData]
    public string? Notes { get; private set; }

    public override string AuditEntityType => "PATIENT_EVENT";

    public void Update(PatientEventType eventType, string title, DateTimeOffset scheduledAt, int? durationMinutes,
        string? locationName, string? providerName, string? notes, Guid userId)
    {
        if (durationMinutes is <= 0)
            throw new BusinessRuleException("INVALID_DURATION", "A duração deve ser maior que zero.");
        EventType = eventType;
        Title = title.Trim();
        ScheduledAt = scheduledAt;
        DurationMinutes = durationMinutes;
        LocationName = locationName?.Trim();
        ProviderName = providerName?.Trim();
        Notes = notes?.Trim();
        TouchBy(userId);
    }

    /// <summary>Altera o status respeitando as transições permitidas.</summary>
    public void ChangeStatus(PatientEventStatus newStatus, string? summary, Guid userId)
    {
        if (newStatus == Status) return;
        var allowed = Status switch
        {
            PatientEventStatus.Scheduled => newStatus is PatientEventStatus.Completed or PatientEventStatus.Cancelled or PatientEventStatus.Missed,
            PatientEventStatus.Cancelled or PatientEventStatus.Missed => newStatus == PatientEventStatus.Scheduled,
            _ => false
        };
        if (!allowed)
            throw new BusinessRuleException("INVALID_STATUS_TRANSITION", $"Não é permitido alterar o evento de {Status} para {newStatus}.");
        Status = newStatus;
        if (summary is not null) Summary = summary.Trim();
        TouchBy(userId);
    }

    /// <summary>Registra/edita o resumo da ocasião após a realização.</summary>
    public void SetSummary(string? summary, Guid userId)
    {
        Summary = summary?.Trim();
        TouchBy(userId);
    }

    /// <summary>Fim previsto do compromisso (usado em agenda).</summary>
    public DateTimeOffset? EndsAt => DurationMinutes.HasValue ? ScheduledAt.AddMinutes(DurationMinutes.Value) : null;
}
