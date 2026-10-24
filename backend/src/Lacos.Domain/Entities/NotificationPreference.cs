using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Preferências de notificação de cada usuário para cada paciente (tabela <c>notification_preferences</c>) — RF12 / UC10.
///
/// O modelo de dados não possuía coluna para "métrica de saúde fora do limite", categoria exigida por RF12;
/// foi acrescentada <see cref="HealthThresholdAlert"/>.
/// SUPOSIÇÃO: ao receber acesso a um paciente, o usuário recebe preferências com todas as categorias habilitadas,
/// podendo desativá-las depois (opt-out), para que nenhum alerta importante passe despercebido por padrão.
/// </summary>
public class NotificationPreference : BaseEntity, IAuditable
{
    private NotificationPreference() { }

    public NotificationPreference(Guid patientId, Guid userId)
    {
        PatientId = patientId;
        UserId = userId;
        MedicationReminder = true;
        MedicationMissed = true;
        AppointmentReminder = true;
        CareRoutineCheckin = true;
        PatientDataAdded = true;
        HealthThresholdAlert = true;
        IsActive = true;
    }

    public Guid PatientId { get; private set; }
    public Guid UserId { get; private set; }
    public bool MedicationReminder { get; private set; }
    public bool MedicationMissed { get; private set; }
    public bool AppointmentReminder { get; private set; }
    public bool CareRoutineCheckin { get; private set; }
    public bool PatientDataAdded { get; private set; }
    public bool HealthThresholdAlert { get; private set; }
    public bool IsActive { get; private set; }

    public string AuditEntityType => "NOTIFICATION_PREFERENCE";
    public Guid? AuditPatientId => PatientId;

    public void Update(bool medicationReminder, bool medicationMissed, bool appointmentReminder,
        bool careRoutineCheckin, bool patientDataAdded, bool healthThresholdAlert, bool isActive)
    {
        MedicationReminder = medicationReminder;
        MedicationMissed = medicationMissed;
        AppointmentReminder = appointmentReminder;
        CareRoutineCheckin = careRoutineCheckin;
        PatientDataAdded = patientDataAdded;
        HealthThresholdAlert = healthThresholdAlert;
        IsActive = isActive;
    }

    /// <summary>Desativa as preferências (ex.: acesso revogado).</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reativa as preferências (ex.: acesso concedido novamente).</summary>
    public void Activate() => IsActive = true;

    /// <summary>Indica se o usuário deseja receber notificações da categoria informada.</summary>
    public bool Accepts(NotificationCategory category) => IsActive && category switch
    {
        NotificationCategory.MedicationReminder => MedicationReminder,
        NotificationCategory.MedicationMissed => MedicationMissed,
        NotificationCategory.AppointmentReminder => AppointmentReminder,
        NotificationCategory.CareRoutineCheckin => CareRoutineCheckin,
        NotificationCategory.PatientDataAdded => PatientDataAdded,
        NotificationCategory.HealthThresholdAlert => HealthThresholdAlert,
        _ => false
    };
}
