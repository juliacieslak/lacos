namespace Lacos.Domain.Enums;

/// <summary>
/// Tipo do compromisso da agenda (patient_events.event_type). Exemplos do modelo: APPOINTMENT, EXAM ou TREATMENT.
/// RF18 menciona "consultas, exames e outras atividades", por isso existe também <see cref="Other"/>.
/// </summary>
public enum PatientEventType
{
    Appointment,
    Exam,
    Treatment,
    Other
}
