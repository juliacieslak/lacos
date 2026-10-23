namespace Lacos.Domain.Enums;

/// <summary>Situação do evento (patient_events.status): SCHEDULED, COMPLETED, CANCELLED ou MISSED.</summary>
public enum PatientEventStatus
{
    Scheduled,
    Completed,
    Cancelled,
    Missed
}
