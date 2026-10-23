using System.Globalization;
using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Application.Services.Clinical;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Tela principal / Dashboard de acompanhamento (WF01).
/// Atende a decisão da validação com stakeholders (Sprint 3): priorizar a visualização dos próximos cuidados,
/// destacando horários, medicamentos pendentes e atividades concluídas.
/// </summary>
public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(Guid? patientId, CancellationToken ct = default);
}

public sealed class DashboardService : IDashboardService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ICurrentUserContext _userContext;
    private readonly IPatientAccessService _access;
    private readonly IPatientService _patients;
    private readonly ICareTeamService _careTeam;
    private readonly IAgendaBuilder _agenda;
    private readonly IHealthMeasurementRepository _measurements;
    private readonly INotificationRepository _notifications;
    private readonly IDateTimeProvider _clock;

    public DashboardService(ICurrentUserContext userContext, IPatientAccessService access, IPatientService patients,
        ICareTeamService careTeam, IAgendaBuilder agenda, IHealthMeasurementRepository measurements,
        INotificationRepository notifications, IDateTimeProvider clock)
    {
        _userContext = userContext;
        _access = access;
        _patients = patients;
        _careTeam = careTeam;
        _agenda = agenda;
        _measurements = measurements;
        _notifications = notifications;
        _clock = clock;
    }

    /// <summary>
    /// Fluxo:
    /// 1. Lista os pacientes acompanhados (com resumo).
    /// 2. Define o paciente selecionado: o informado, ou o único paciente quando houver apenas um.
    /// 3. Agenda de hoje do paciente selecionado (ou de todos), com contagem de pendentes e concluídos.
    /// 4. Últimas métricas (pressão, glicemia etc.) e rede de cuidado do paciente selecionado.
    /// 5. Quantidade de notificações não lidas (sino do header).
    /// </summary>
    public async Task<DashboardResponse> GetAsync(Guid? patientId, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        var summaries = await _patients.ListMineAsync(null, null, ct);

        Guid? selected = patientId ?? (summaries.Count == 1 ? summaries[0].Id : null);
        if (patientId is { } requested) await _access.RequireAsync(requested, PatientPermission.View, ct);

        var scope = selected is { } s
            ? summaries.Where(p => p.Id == s).ToDictionary(p => p.Id, p => p.FullName)
            : summaries.ToDictionary(p => p.Id, p => p.FullName);
        var today = _clock.LocalToday;
        var agenda = await _agenda.BuildAsync(scope, today, today, ct);

        IReadOnlyList<LatestMetricDto> metrics = Array.Empty<LatestMetricDto>();
        IReadOnlyList<CareTeamMemberResponse> team = Array.Empty<CareTeamMemberResponse>();
        if (selected is { } patient)
        {
            metrics = BuildLatestMetrics(await _measurements.ListLatestAsync(patient, 30, ct));
            team = await _careTeam.ListMembersAsync(patient, includeInactive: false, ct);
        }

        var (_, _, unread) = await _notifications.ListByUserAsync(user.Id, unreadOnly: true, 0, 1, ct);
        return new DashboardResponse(today, user.Name, summaries, selected, agenda, agenda.Count(i => !i.Done),
            agenda.Count(i => i.Done), metrics, team, unread);
    }

    /// <summary>Para cada métrica, pega o valor mais recente entre as últimas aferições (lista já ordenada desc).</summary>
    private static IReadOnlyList<LatestMetricDto> BuildLatestMetrics(IReadOnlyList<HealthMeasurement> latest)
    {
        var result = new List<LatestMetricDto>();
        void Add(HealthMetric? metric, string name, Func<HealthMeasurement, string?> value)
        {
            var m = latest.FirstOrDefault(x => value(x) is not null);
            if (m is not null) result.Add(new LatestMetricDto(metric, name, value(m)!, m.MeasuredAt));
        }

        Add(HealthMetric.SystolicPressure, "Pressão arterial", m => m.Systolic is null ? null : $"{m.Systolic}/{m.Diastolic} mmHg");
        Add(HealthMetric.BloodGlucose, MetricNames.Of(HealthMetric.BloodGlucose),
            m => m.BloodGlucose is null ? null : $"{m.BloodGlucose.Value.ToString("0.##", PtBr)} {(m.GlucoseUnit == GlucoseUnit.MgPerDl ? "mg/dL" : "mmol/L")}");
        Add(HealthMetric.HeartRate, MetricNames.Of(HealthMetric.HeartRate), m => m.HeartRate is null ? null : $"{m.HeartRate} bpm");
        Add(HealthMetric.Temperature, MetricNames.Of(HealthMetric.Temperature),
            m => m.Temperature is null ? null : $"{m.Temperature.Value.ToString("0.0", PtBr)} {(m.TemperatureUnit == TemperatureUnit.Celsius ? "°C" : "°F")}");
        Add(HealthMetric.OxygenSaturation, MetricNames.Of(HealthMetric.OxygenSaturation), m => m.OxygenSaturation is null ? null : $"{m.OxygenSaturation}%");
        Add(HealthMetric.RespiratoryRate, MetricNames.Of(HealthMetric.RespiratoryRate), m => m.RespiratoryRate is null ? null : $"{m.RespiratoryRate} rpm");
        Add(null, "Peso", m => m.Weight is null ? null : $"{m.Weight.Value.ToString("0.##", PtBr)} {(m.WeightUnit == WeightUnit.Kilogram ? "kg" : "lb")}");
        Add(null, "Nível de dor", m => m.PainLevel is null ? null : $"{m.PainLevel}/10");
        return result;
    }
}
