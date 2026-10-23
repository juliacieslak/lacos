namespace Lacos.Domain.Enums;

/// <summary>
/// Categorias de notificação que o usuário pode escolher receber (RF12 e notification_preferences).
/// </summary>
public enum NotificationCategory
{
    /// <summary>Lembrete de medicamento programado (medication_reminder).</summary>
    MedicationReminder,
    /// <summary>Medicação não registrada no período esperado (medication_missed).</summary>
    MedicationMissed,
    /// <summary>Lembrete antes de eventos da agenda (appointment_reminder).</summary>
    AppointmentReminder,
    /// <summary>Lembrete periódico de rotinas de cuidado/aferições (care_routine_checkin).</summary>
    CareRoutineCheckin,
    /// <summary>Nova informação adicionada ao paciente (patient_data_added).</summary>
    PatientDataAdded,
    /// <summary>Métrica de saúde fora do limite estabelecido (RF12/RF13 - health_threshold_alert).</summary>
    HealthThresholdAlert
}
