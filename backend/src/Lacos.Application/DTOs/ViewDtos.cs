using System.Text.Json;
using Lacos.Application.Common;
using Lacos.Domain.Enums;

namespace Lacos.Application.DTOs;

// ---------------- Agenda consolidada (WF03) ----------------
public enum AgendaView { Day, Week, Month }

public enum AgendaItemKind { Event, Medication, CareTask }

/// <summary>
/// Item da agenda consolidada: compromissos (RF18), doses programadas (RF08) e tarefas da rotina.
/// <see cref="Done"/> indica se o item foi concluído (checkbox do wireframe).
/// </summary>
public sealed record AgendaItemResponse(
    AgendaItemKind Kind, Guid SourceId, Guid PatientId, string PatientName, string Title, string? Subtitle,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool Done, bool Overdue,
    PatientEventStatus? EventStatus, PatientEventType? EventType, string? LocationName, string? ProviderName,
    Guid? AdministrationId, MedicationAdministrationStatus? AdministrationStatus,
    DateOnly? TaskDate, Guid? CompletionId);

public sealed record AgendaResponse(DateOnly From, DateOnly To, AgendaView View, IReadOnlyList<AgendaItemResponse> Items);

// ---------------- Dashboard (WF01) ----------------
public sealed record LatestMetricDto(HealthMetric? Metric, string Name, string Value, DateTimeOffset MeasuredAt);

public sealed record DashboardResponse(
    DateOnly Date,
    string UserName,
    IReadOnlyList<PatientSummaryResponse> Patients,
    Guid? SelectedPatientId,
    IReadOnlyList<AgendaItemResponse> TodayAgenda,
    int TodayPending,
    int TodayCompleted,
    IReadOnlyList<LatestMetricDto> LatestMetrics,
    IReadOnlyList<CareTeamMemberResponse> CareTeam,
    int UnreadNotifications);

// ---------------- Linha do tempo (US04) ----------------
public sealed record TimelineItemResponse(
    TimelineCategory Category, Guid EntityId, DateTimeOffset OccurredAt, string Title, string? Description,
    Guid RecordedByUserId, string? RecordedByName, object Details);

// ---------------- Histórico / auditoria (RF06) ----------------
public sealed record AuditLogResponse(
    Guid Id, Guid? UserId, string? UserName, string EntityType, Guid? EntityId, AuditAction Action,
    JsonElement? OldValues, JsonElement? NewValues, string? Reason, string? IpAddress, DateTimeOffset CreatedAt);

// ---------------- Documentos (RF19) ----------------
public sealed record DocumentResponse(
    Guid Id, Guid PatientId, string FileName, string ContentType, long SizeBytes, DocumentCategory Category,
    string? Description, Guid? RelatedEventId, RecordInfoDto Record);

/// <summary>Comando de upload desacoplado de ASP.NET (IFormFile é convertido no controller).</summary>
public sealed record UploadDocumentCommand(
    Stream Content, string FileName, string ContentType, long Length, DocumentCategory Category,
    string? Description, Guid? RelatedEventId);

public sealed record UpdateDocumentRequest(DocumentCategory Category, string? Description, Guid? RelatedEventId);

public sealed record DocumentContent(byte[] Content, string FileName, string ContentType);

// ---------------- Notificações (RF12) ----------------
public sealed record NotificationResponse(
    Guid Id, Guid? PatientId, NotificationCategory Category, string Title, string Message, Guid? RelatedEntityId,
    DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);

public sealed record NotificationListResponse(PagedResult<NotificationResponse> Page, int UnreadCount);

public sealed record NotificationPreferenceRequest(
    bool MedicationReminder, bool MedicationMissed, bool AppointmentReminder, bool CareRoutineCheckin,
    bool PatientDataAdded, bool HealthThresholdAlert, bool IsActive);

public sealed record NotificationPreferenceResponse(
    Guid PatientId, string PatientName, bool MedicationReminder, bool MedicationMissed, bool AppointmentReminder,
    bool CareRoutineCheckin, bool PatientDataAdded, bool HealthThresholdAlert, bool IsActive);
