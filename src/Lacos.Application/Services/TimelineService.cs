using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Domain.Common;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Linha do tempo consolidada do paciente (US04 / UC09 "Consultar Histórico do Paciente" / WF02 "Histórico clínico").
/// Critérios de aceitação atendidos: ordem cronológica decrescente, filtro por categoria, exibição de quem realizou
/// cada registro e acesso aos documentos anexados (por meio do EntityId do documento).
/// </summary>
public interface ITimelineService
{
    Task<PagedResult<TimelineItemResponse>> GetAsync(Guid patientId, IReadOnlyCollection<TimelineCategory>? categories,
        DateTimeOffset? from, DateTimeOffset? to, PageQuery page, CancellationToken ct = default);
}

public sealed class TimelineService : ITimelineService
{
    private readonly IPatientAccessService _access;
    private readonly IAuditService _audit;
    private readonly IUserRepository _users;
    private readonly IMedicationRepository _medications;
    private readonly IMedicationAdministrationRepository _administrations;
    private readonly IHealthMeasurementRepository _measurements;
    private readonly IPatientRecordRepository<SymptomRecord> _symptoms;
    private readonly IPatientRecordRepository<CareDiaryEntry> _diary;
    private readonly IPatientEventRepository _events;
    private readonly IPatientRecordRepository<PatientDocument> _documents;
    private readonly IHealthConditionRepository _conditions;
    private readonly IPatientRecordRepository<Surgery> _surgeries;
    private readonly IPatientRecordRepository<Allergy> _allergies;
    private readonly IDateTimeProvider _clock;

    public TimelineService(IPatientAccessService access, IAuditService audit, IUserRepository users, IMedicationRepository medications,
        IMedicationAdministrationRepository administrations, IHealthMeasurementRepository measurements,
        IPatientRecordRepository<SymptomRecord> symptoms, IPatientRecordRepository<CareDiaryEntry> diary, IPatientEventRepository events,
        IPatientRecordRepository<PatientDocument> documents, IHealthConditionRepository conditions,
        IPatientRecordRepository<Surgery> surgeries, IPatientRecordRepository<Allergy> allergies, IDateTimeProvider clock)
    {
        _access = access;
        _audit = audit;
        _users = users;
        _medications = medications;
        _administrations = administrations;
        _measurements = measurements;
        _symptoms = symptoms;
        _diary = diary;
        _events = events;
        _documents = documents;
        _conditions = conditions;
        _surgeries = surgeries;
        _allergies = allergies;
        _clock = clock;
    }

    /// <summary>
    /// Algoritmo (merge paginado de múltiplas fontes):
    /// 1. Para cada categoria selecionada, busca os N primeiros registros mais recentes do período, onde
    ///    N = página * tamanho (o suficiente para compor a página pedida, qualquer que seja a distribuição entre fontes).
    /// 2. Junta as fontes, ordena por data decrescente e recorta a página.
    /// 3. O total é a soma dos totais de cada fonte.
    /// Observação de escalabilidade: para históricos muito grandes, uma visão materializada (UNION ALL) no banco
    /// pode substituir este merge sem alterar o contrato do serviço.
    /// </summary>
    public async Task<PagedResult<TimelineItemResponse>> GetAsync(Guid patientId, IReadOnlyCollection<TimelineCategory>? categories,
        DateTimeOffset? from, DateTimeOffset? to, PageQuery page, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.View, ct);
        var selected = categories is { Count: > 0 } ? categories.ToHashSet() : Enum.GetValues<TimelineCategory>().ToHashSet();
        var take = page.SafePage * page.SafePageSize;
        var items = new List<TimelineItemResponse>();
        var total = 0;

        async Task Collect<T>(TimelineCategory category, IPatientRecordRepository<T> repo, Func<T, TimelineItemResponse> map) where T : PatientRecord
        {
            if (!selected.Contains(category)) return;
            var (list, count) = await repo.ListByPatientAsync(patientId, from, to, 0, take, ct);
            total += count;
            items.AddRange(list.Select(map));
        }

        var medNames = new Dictionary<Guid, string>();
        await Collect(TimelineCategory.Medication, _medications, m =>
        {
            medNames[m.Id] = m.Name;
            return Item(TimelineCategory.Medication, m, m.CreatedAt, $"Medicamento: {m.Name}", $"{m.Dosage} — {m.Frequency}",
                new { m.Name, m.Dosage, m.Brand, m.AdministrationRoute, m.StartDate, m.EndDate, m.Frequency, m.AdministrationTimes });
        });
        await Collect(TimelineCategory.MedicationAdministration, _administrations, a =>
            Item(TimelineCategory.MedicationAdministration, a, a.AdministeredAt,
                a.Status == MedicationAdministrationStatus.Administered ? "Medicamento administrado" : "Medicamento não administrado",
                a.Notes, new { a.MedicationId, a.ScheduledFor, a.AdministeredAt, a.Status }));
        await Collect(TimelineCategory.Measurement, _measurements, m =>
            Item(TimelineCategory.Measurement, m, m.MeasuredAt, "Aferição de sinais vitais", m.Description,
                new { m.Systolic, m.Diastolic, m.HeartRate, m.Temperature, m.TemperatureUnit, m.RespiratoryRate, m.OxygenSaturation,
                      m.BloodGlucose, m.GlucoseUnit, m.PainLevel, m.Weight, m.WeightUnit }));
        await Collect(TimelineCategory.Symptom, _symptoms, s =>
            Item(TimelineCategory.Symptom, s, s.OccurredAt, s.Type switch
            {
                SymptomType.Complaint => "Queixa",
                SymptomType.HealthChange => "Alteração no estado de saúde",
                _ => "Sintoma"
            }, s.Description, new { s.Type }));
        await Collect(TimelineCategory.CareDiary, _diary, d =>
            Item(TimelineCategory.CareDiary, d, d.OccurredAt, $"Diário: {d.Category}", d.Description, new { d.Category }));
        await Collect(TimelineCategory.Event, _events, e =>
            Item(TimelineCategory.Event, e, e.ScheduledAt, e.Title, e.Summary ?? e.Notes,
                new { e.EventType, e.Status, e.DurationMinutes, e.LocationName, e.ProviderName }));
        await Collect(TimelineCategory.Document, _documents, d =>
            Item(TimelineCategory.Document, d, d.CreatedAt, $"Documento: {d.FileName}", d.Description,
                new { d.Category, d.ContentType, d.SizeBytes, d.RelatedEventId }));
        await Collect(TimelineCategory.HealthCondition, _conditions, c =>
            Item(TimelineCategory.HealthCondition, c, ToInstant(c.DiagnosisDate) ?? c.CreatedAt, $"Condição: {c.Name} ({c.CidCode})", c.Notes,
                new { c.CidCode, c.Status, c.DiagnosisDate }));
        await Collect(TimelineCategory.Surgery, _surgeries, s =>
            Item(TimelineCategory.Surgery, s, ToInstant(s.PerformedOn) ?? s.CreatedAt, $"Cirurgia: {s.ProcedureName}", s.Reason,
                new { s.PerformedOn }));
        await Collect(TimelineCategory.Allergy, _allergies, a =>
            Item(TimelineCategory.Allergy, a, ToInstant(a.IdentifiedOn) ?? a.CreatedAt, $"Alergia: {a.CausativeAgent}", a.Manifestation,
                new { a.Severity, a.IdentifiedOn }));

        var pageItems = items.OrderByDescending(i => i.OccurredAt).Skip(page.Skip).Take(page.SafePageSize).ToList();

        // Completa nomes dos responsáveis (US04: "Exibe quem realizou cada um dos registros").
        var names = await _users.GetNamesAsync(pageItems.Select(i => i.RecordedByUserId), ct);
        pageItems = pageItems.Select(i => i with { RecordedByName = names.GetValueOrDefault(i.RecordedByUserId) }).ToList();

        await _audit.RecordViewAsync(access.UserId, patientId, "PATIENT_TIMELINE", null, ct);
        return new PagedResult<TimelineItemResponse>(pageItems, page.SafePage, page.SafePageSize, total);
    }

    private DateTimeOffset? ToInstant(DateOnly? date) => date is { } d ? _clock.ToInstant(d, new TimeOnly(12, 0)) : null;

    private static TimelineItemResponse Item(TimelineCategory category, PatientRecord record, DateTimeOffset occurredAt,
        string title, string? description, object details) =>
        new(category, record.Id, occurredAt, title, description, record.CreatedByUserId, null, details);
}
