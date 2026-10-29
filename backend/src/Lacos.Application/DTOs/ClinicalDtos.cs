using Lacos.Domain.Enums;

namespace Lacos.Application.DTOs;

// ---------------- Medicamentos (RF08) ----------------
public sealed record MedicationRequest(
    string Name, string Dosage, string? Brand, string? UsageReason, AdministrationRoute AdministrationRoute,
    string? PrescribedBy, DateOnly StartDate, DateOnly? EndDate, string Frequency,
    IReadOnlyList<TimeOnly>? AdministrationTimes, IReadOnlyList<DayOfWeek>? DaysOfWeek);

public sealed record MedicationResponse(
    Guid Id, Guid PatientId, string Name, string Dosage, string? Brand, string? UsageReason,
    AdministrationRoute AdministrationRoute, string? PrescribedBy, DateOnly StartDate, DateOnly? EndDate,
    string Frequency, IReadOnlyList<TimeOnly> AdministrationTimes, IReadOnlyList<DayOfWeek> DaysOfWeek,
    bool IsActiveToday, RecordInfoDto Record);

// ---------------- Administração de medicamentos (US02) ----------------
/// <summary>
/// Registro rápido de dose (validação com cuidador - Sprint 3). Se <see cref="AdministeredAt"/> for omitido,
/// é usada a data/hora atual (US02: "Associa automaticamente a data, hora...").
/// </summary>
public sealed record MedicationAdministrationRequest(
    Guid MedicationId, DateTimeOffset? ScheduledFor, DateTimeOffset? AdministeredAt,
    MedicationAdministrationStatus Status, string? Notes);

public sealed record UpdateMedicationAdministrationRequest(
    DateTimeOffset AdministeredAt, MedicationAdministrationStatus Status, string? Notes);

public sealed record MedicationAdministrationResponse(
    Guid Id, Guid PatientId, Guid MedicationId, string? MedicationName, DateTimeOffset? ScheduledFor,
    DateTimeOffset AdministeredAt, MedicationAdministrationStatus Status, string? Notes, RecordInfoDto Record);

// ---------------- Métricas de saúde (RF09) ----------------
/// <summary>Unidades omitidas assumem as unidades configuradas no paciente (RF09).</summary>
public sealed record HealthMeasurementRequest(
    DateTimeOffset? MeasuredAt, short? Systolic, short? Diastolic, short? HeartRate,
    decimal? Temperature, TemperatureUnit? TemperatureUnit, short? RespiratoryRate, short? OxygenSaturation,
    decimal? BloodGlucose, GlucoseUnit? GlucoseUnit, short? PainLevel, decimal? Weight, WeightUnit? WeightUnit,
    string? Description);

public sealed record ThresholdAlertDto(HealthMetric Metric, decimal Value, decimal Limit, bool IsAboveMax);

public sealed record HealthMeasurementResponse(
    Guid Id, Guid PatientId, DateTimeOffset MeasuredAt, short? Systolic, short? Diastolic, short? HeartRate,
    decimal? Temperature, TemperatureUnit TemperatureUnit, short? RespiratoryRate, short? OxygenSaturation,
    decimal? BloodGlucose, GlucoseUnit GlucoseUnit, short? PainLevel, decimal? Weight, WeightUnit WeightUnit,
    string? Description, IReadOnlyList<ThresholdAlertDto> Alerts, RecordInfoDto Record);

// ---------------- Limites (RF13) ----------------
public sealed record MetricLimitDto(decimal? Min, bool MinNotice, decimal? Max, bool MaxNotice);

public sealed record HealthThresholdRequest(
    MetricLimitDto? SystolicPressure, MetricLimitDto? DiastolicPressure, MetricLimitDto? HeartRate,
    MetricLimitDto? Temperature, MetricLimitDto? RespiratoryRate, MetricLimitDto? OxygenSaturation,
    MetricLimitDto? BloodGlucose);

public sealed record HealthThresholdResponse(
    Guid? Id, Guid PatientId, MetricLimitDto SystolicPressure, MetricLimitDto DiastolicPressure, MetricLimitDto HeartRate,
    MetricLimitDto Temperature, MetricLimitDto RespiratoryRate, MetricLimitDto OxygenSaturation, MetricLimitDto BloodGlucose,
    Guid? CreatedByUserId, Guid? UpdatedByUserId, DateTimeOffset? UpdatedAt);

// ---------------- Sintomas (RF09) ----------------
public sealed record SymptomRequest(SymptomType Type, string Description, DateTimeOffset? OccurredAt);
public sealed record SymptomResponse(Guid Id, Guid PatientId, SymptomType Type, string Description, DateTimeOffset OccurredAt, RecordInfoDto Record);

// ---------------- Diário (RF04) ----------------
public sealed record CareDiaryRequest(CareDiaryCategory Category, string Description, DateTimeOffset? OccurredAt);
public sealed record CareDiaryResponse(Guid Id, Guid PatientId, CareDiaryCategory Category, string Description, DateTimeOffset OccurredAt, RecordInfoDto Record);

// ---------------- Condições de saúde (RF14) ----------------
public sealed record HealthConditionRequest(string Name, string CidCode, DateOnly? DiagnosisDate, HealthConditionStatus Status, string? Notes);
public sealed record HealthConditionResponse(Guid Id, Guid PatientId, string Name, string CidCode, DateOnly? DiagnosisDate,
    HealthConditionStatus Status, string? Notes, RecordInfoDto Record);

// ---------------- Cirurgias (RF15) ----------------
public sealed record SurgeryRequest(string ProcedureName, DateOnly PerformedOn, string Reason);
public sealed record SurgeryResponse(Guid Id, Guid PatientId, string ProcedureName, DateOnly PerformedOn, string Reason, RecordInfoDto Record);

// ---------------- Alergias (RF16) ----------------
public sealed record AllergyRequest(string CausativeAgent, string Manifestation, DateOnly? IdentifiedOn, AllergySeverity Severity);
public sealed record AllergyResponse(Guid Id, Guid PatientId, string CausativeAgent, string Manifestation, DateOnly? IdentifiedOn,
    AllergySeverity Severity, RecordInfoDto Record);

// ---------------- Agenda / eventos (RF18) ----------------
public sealed record PatientEventRequest(
    PatientEventType EventType, string Title, DateTimeOffset ScheduledAt, int? DurationMinutes,
    string? LocationName, string? ProviderName, string? Notes);

public sealed record ChangeEventStatusRequest(PatientEventStatus Status, string? Summary);

public sealed record EventSummaryRequest(string? Summary);

public sealed record PatientEventResponse(
    Guid Id, Guid PatientId, PatientEventType EventType, string Title, DateTimeOffset ScheduledAt, int? DurationMinutes,
    DateTimeOffset? EndsAt, string? LocationName, string? ProviderName, PatientEventStatus Status, string? Summary,
    string? Notes, RecordInfoDto Record);

// ---------------- Rotina de cuidado (WF01/WF03) ----------------
public sealed record CareRoutineTaskRequest(
    string Title, string? Description, CareDiaryCategory Category, TimeOnly ScheduledTime,
    IReadOnlyList<DayOfWeek>? DaysOfWeek, DateOnly? StartDate, DateOnly? EndDate);

public sealed record CareRoutineTaskResponse(
    Guid Id, Guid PatientId, string Title, string? Description, CareDiaryCategory Category, TimeOnly ScheduledTime,
    IReadOnlyList<DayOfWeek> DaysOfWeek, DateOnly StartDate, DateOnly? EndDate, RecordInfoDto Record);

public sealed record CompleteCareTaskRequest(DateOnly? Date, string? Notes);

public sealed record CareRoutineCompletionResponse(Guid Id, Guid TaskId, DateOnly ScheduledDate, DateTimeOffset CompletedAt,
    string? Notes, Guid CompletedByUserId);
