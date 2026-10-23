using Lacos.Application.DTOs;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services.Clinical;

/// <summary>
/// Registro de métricas de saúde (RF09) com avaliação automática dos limites clínicos (RF13)
/// e geração de alertas para quem optou por recebê-los (RF12 / RNF11).
/// </summary>
public sealed class HealthMeasurementService : PatientRecordService<HealthMeasurement, HealthMeasurementRequest, HealthMeasurementResponse>
{
    private readonly IHealthThresholdRepository _thresholds;

    public HealthMeasurementService(IHealthMeasurementRepository repository, IHealthThresholdRepository thresholds,
        PatientRecordDependencies deps) : base(repository, deps) => _thresholds = thresholds;

    protected override string ResourceName => "Métrica de saúde";

    protected override Task<HealthMeasurement> CreateEntityAsync(PatientAccess access, HealthMeasurementRequest r, CancellationToken ct)
    {
        var data = ToData(access.Patient, r, NowIfNull(r.MeasuredAt));
        EnsureNotInFuture(data.MeasuredAt, nameof(r.MeasuredAt));
        return Task.FromResult(new HealthMeasurement(access.Patient.Id, access.UserId, data));
    }

    protected override Task ApplyUpdateAsync(PatientAccess access, HealthMeasurement e, HealthMeasurementRequest r, CancellationToken ct)
    {
        var data = ToData(access.Patient, r, r.MeasuredAt ?? e.MeasuredAt);
        EnsureNotInFuture(data.MeasuredAt, nameof(r.MeasuredAt));
        e.Update(data, access.UserId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Após incluir/alterar: avalia cada métrica contra os limites do paciente e, para cada violação com alerta habilitado,
    /// enfileira uma notificação HEALTH_THRESHOLD_ALERT. A chave de deduplicação inclui a métrica e o valor,
    /// assim a correção de um valor gera novo alerta, mas reprocessar o mesmo valor não.
    /// </summary>
    protected override async Task AfterChangeAsync(PatientAccess access, HealthMeasurement entity, bool created, CancellationToken ct)
    {
        var threshold = await _thresholds.GetByPatientAsync(access.Patient.Id, ct);
        if (threshold is null) return;

        foreach (var (metric, value) in entity.GetCanonicalValues())
        {
            var breach = threshold.Evaluate(metric, value);
            if (breach is null) continue;
            var direction = breach.IsAboveMax ? "atingiu ou ultrapassou o limite máximo" : "atingiu ou ficou abaixo do limite mínimo";
            await Deps.Notifications.EnqueueForPatientAsync(access.Patient.Id, NotificationCategory.HealthThresholdAlert,
                $"Alerta: {MetricNames.Of(metric)} fora do limite",
                $"{MetricNames.Of(metric)} de {access.Patient.FullName} {direction} ({breach.Value} / limite {breach.Limit}).",
                $"threshold:{entity.Id:N}:{metric}:{breach.Value}", entity.Id, excludeUserId: null, ct);
        }
    }

    protected override HealthMeasurementResponse Map(HealthMeasurement e, IReadOnlyDictionary<Guid, string> names) =>
        Map(e, names, Array.Empty<ThresholdAlertDto>());

    // Inclui os alertas atuais na resposta para que a interface destaque valores fora do limite.
    protected override async Task<HealthMeasurementResponse> MapAsync(HealthMeasurement e, CancellationToken ct)
    {
        var names = await LoadNamesAsync(new[] { e }, ct);
        var threshold = await _thresholds.GetByPatientAsync(e.PatientId, ct);
        return Map(e, names, Evaluate(threshold, e));
    }

    protected override async Task<HealthMeasurementResponse> MapListItemAsync(HealthMeasurement e, IReadOnlyDictionary<Guid, string> names, CancellationToken ct)
    {
        var threshold = await _thresholds.GetByPatientAsync(e.PatientId, ct);
        return Map(e, names, Evaluate(threshold, e));
    }

    private static IReadOnlyList<ThresholdAlertDto> Evaluate(HealthThreshold? threshold, HealthMeasurement e) =>
        threshold is null
            ? Array.Empty<ThresholdAlertDto>()
            : e.GetCanonicalValues().Select(v => threshold.Evaluate(v.Metric, v.Value)).Where(b => b is not null)
                .Select(b => new ThresholdAlertDto(b!.Metric, b.Value, b.Limit, b.IsAboveMax)).ToList();

    private static HealthMeasurementResponse Map(HealthMeasurement e, IReadOnlyDictionary<Guid, string> names, IReadOnlyList<ThresholdAlertDto> alerts) =>
        new(e.Id, e.PatientId, e.MeasuredAt, e.Systolic, e.Diastolic, e.HeartRate, e.Temperature, e.TemperatureUnit,
            e.RespiratoryRate, e.OxygenSaturation, e.BloodGlucose, e.GlucoseUnit, e.PainLevel, e.Weight, e.WeightUnit,
            e.Description, alerts, e.ToRecordInfo(names));

    // Unidades não informadas assumem a configuração do paciente (RF09).
    private static MeasurementData ToData(Patient p, HealthMeasurementRequest r, DateTimeOffset measuredAt) =>
        new(measuredAt, r.Systolic, r.Diastolic, r.HeartRate, r.Temperature, r.TemperatureUnit ?? p.TemperatureUnit,
            r.RespiratoryRate, r.OxygenSaturation, r.BloodGlucose, r.GlucoseUnit ?? p.GlucoseUnit, r.PainLevel,
            r.Weight, r.WeightUnit ?? p.WeightUnit, r.Description);
}

/// <summary>Nomes amigáveis das métricas para mensagens.</summary>
public static class MetricNames
{
    public static string Of(HealthMetric metric) => metric switch
    {
        HealthMetric.SystolicPressure => "Pressão sistólica",
        HealthMetric.DiastolicPressure => "Pressão diastólica",
        HealthMetric.HeartRate => "Frequência cardíaca",
        HealthMetric.Temperature => "Temperatura",
        HealthMetric.RespiratoryRate => "Frequência respiratória",
        HealthMetric.OxygenSaturation => "Saturação de oxigênio",
        HealthMetric.BloodGlucose => "Glicemia",
        _ => metric.ToString()
    };
}
