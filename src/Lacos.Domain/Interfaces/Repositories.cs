using Lacos.Domain.Entities;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByAuth0IdAsync(string auth0UserId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByCpfHashAsync(string cpfHash, Guid? exceptUserId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}

public interface IPatientRepository : IRepository<Patient>
{
    Task<bool> ExistsByCpfHashAsync(string cpfHash, Guid? exceptPatientId, CancellationToken ct = default);

    /// <summary>Lista os pacientes aos quais o usuário tem vínculo ativo com permissão de visualização (RF11).</summary>
    Task<IReadOnlyList<(Patient Patient, PatientUserPermission Permission)>> ListAccessibleAsync(Guid userId, string? search,
        PatientCareStatus? status, CancellationToken ct = default);
}

public interface IPatientPermissionRepository : IRepository<PatientUserPermission>
{
    /// <summary>Retorna o vínculo (ativo ou não) entre usuário e paciente.</summary>
    Task<PatientUserPermission?> GetAsync(Guid patientId, Guid userId, CancellationToken ct = default);
    /// <summary>Lista os membros da rede de cuidado com os dados do usuário (US01 "Quem está cuidando").</summary>
    Task<IReadOnlyList<PatientUserPermission>> ListByPatientAsync(Guid patientId, bool includeInactive, CancellationToken ct = default);
    Task<IReadOnlyList<PatientUserPermission>> ListActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountActiveManagersAsync(Guid patientId, CancellationToken ct = default);
}

public interface IPatientInvitationRepository : IRepository<PatientInvitation>
{
    Task<IReadOnlyList<PatientInvitation>> ListPendingByEmailAsync(string email, DateTimeOffset now, CancellationToken ct = default);
    Task<IReadOnlyList<PatientInvitation>> ListByPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<bool> ExistsPendingAsync(Guid patientId, string email, DateTimeOffset now, CancellationToken ct = default);
}

public interface IHealthThresholdRepository : IRepository<HealthThreshold>
{
    Task<HealthThreshold?> GetByPatientAsync(Guid patientId, CancellationToken ct = default);
}

public interface INotificationPreferenceRepository : IRepository<NotificationPreference>
{
    Task<NotificationPreference?> GetAsync(Guid patientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Destinatários de uma categoria para um paciente: usuários com vínculo ativo que podem visualizar o paciente
    /// e cuja preferência aceita a categoria.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListRecipientsAsync(Guid patientId, NotificationCategory category, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    /// <summary>Somente inclusão: a trilha é imutável (RNF04).</summary>
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, int Total)> ListByPatientAsync(Guid patientId, string? entityType, Guid? entityId,
        AuditAction? action, DateTimeOffset? from, DateTimeOffset? to, bool includeViews, int skip, int take, CancellationToken ct = default);
}

public interface INotificationRepository : IRepository<Notification>
{
    Task<bool> ExistsByDeduplicationKeyAsync(string key, CancellationToken ct = default);
    Task<(IReadOnlyList<Notification> Items, int Total, int Unread)> ListByUserAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListPendingDeliveryAsync(int take, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset when, CancellationToken ct = default);
    /// <summary>Remove fisicamente as notificações do usuário (exclusão de conta - LGPD).</summary>
    Task DeleteByUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IUserConsentRepository : IRepository<UserConsent>
{
    Task<IReadOnlyList<UserConsent>> ListByUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IPatientEventRepository : IPatientRecordRepository<PatientEvent>
{
    Task<IReadOnlyList<PatientEvent>> ListInRangeAsync(IEnumerable<Guid> patientIds, DateTimeOffset from, DateTimeOffset to,
        PatientEventType? type, PatientEventStatus? status, CancellationToken ct = default);
    Task<PatientEvent?> GetNextScheduledAsync(Guid patientId, DateTimeOffset from, CancellationToken ct = default);
    /// <summary>Eventos SCHEDULED de todos os pacientes no período (para os lembretes automáticos).</summary>
    Task<IReadOnlyList<PatientEvent>> ListScheduledBetweenAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}

public interface IMedicationRepository : IPatientRecordRepository<Medication>
{
    Task<IReadOnlyList<Medication>> ListByPatientsAsync(IEnumerable<Guid> patientIds, bool activeOnly, DateOnly referenceDate, CancellationToken ct = default);
    /// <summary>Medicamentos em uso na data em todos os pacientes ativos (para os lembretes automáticos).</summary>
    Task<IReadOnlyList<Medication>> ListActiveOnAsync(DateOnly date, CancellationToken ct = default);
}

public interface IMedicationAdministrationRepository : IPatientRecordRepository<MedicationAdministration>
{
    Task<bool> ExistsForOccurrenceAsync(Guid medicationId, DateTimeOffset scheduledFor, CancellationToken ct = default);
    Task<IReadOnlyList<MedicationAdministration>> ListByMedicationsAsync(IEnumerable<Guid> medicationIds, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}

public interface IHealthMeasurementRepository : IPatientRecordRepository<HealthMeasurement>
{
    /// <summary>Últimas aferições do paciente (para exibição das métricas no dashboard - WF01).</summary>
    Task<IReadOnlyList<HealthMeasurement>> ListLatestAsync(Guid patientId, int take, CancellationToken ct = default);
}

public interface ICareRoutineRepository : IPatientRecordRepository<CareRoutineTask>
{
    Task<IReadOnlyList<CareRoutineTask>> ListByPatientsAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default);
    Task<IReadOnlyList<CareRoutineTask>> ListAllActiveAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<CareRoutineCompletion>> ListCompletionsAsync(IEnumerable<Guid> taskIds, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<CareRoutineCompletion?> GetCompletionAsync(Guid taskId, DateOnly date, CancellationToken ct = default);
    Task AddCompletionAsync(CareRoutineCompletion completion, CancellationToken ct = default);
}

public interface IHealthConditionRepository : IPatientRecordRepository<HealthCondition>
{
    Task<IReadOnlyList<HealthCondition>> ListActiveByPatientsAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default);
}

/// <summary>
/// Exclusão definitiva dos dados de um paciente (LGPD - RNF01).
/// Implementada na infraestrutura porque exige remoção física em várias tabelas e a redação da auditoria.
/// </summary>
public interface IPatientDataEraser
{
    Task EraseClinicalDataAsync(Guid patientId, CancellationToken ct = default);
}
