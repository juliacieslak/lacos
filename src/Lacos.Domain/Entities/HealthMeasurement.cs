using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.ValueObjects;

namespace Lacos.Domain.Entities;

/// <summary>
/// Aferição de métricas de saúde (tabela <c>health_measurements</c>) — RF09.
/// Métricas: pressão (sistólica/diastólica), frequência cardíaca, temperatura, frequência respiratória,
/// saturação, glicemia, nível de dor e peso; com descrição e data/hora da ocorrência.
///
/// Regras:
/// - Ao menos uma métrica deve ser informada.
/// - Pressão arterial exige sistólica e diastólica juntas, com sistólica &gt; diastólica.
/// - Valores são guardados na unidade em que foram informados (unidade do paciente - RF09) e convertidos
///   para a unidade canônica apenas para comparação com os limites (RF13).
/// - Faixas de plausibilidade fisiológica evitam erros de digitação (validadas na camada de aplicação).
/// </summary>
public class HealthMeasurement : PatientRecord
{
    private HealthMeasurement() { }

    public HealthMeasurement(Guid patientId, Guid recordedByUserId, MeasurementData data) : base(patientId, recordedByUserId)
    {
        Apply(data);
    }

    public DateTimeOffset MeasuredAt { get; private set; }
    public short? Systolic { get; private set; }
    public short? Diastolic { get; private set; }
    public short? HeartRate { get; private set; }
    public decimal? Temperature { get; private set; }
    public TemperatureUnit TemperatureUnit { get; private set; }
    public short? RespiratoryRate { get; private set; }
    public short? OxygenSaturation { get; private set; }
    public decimal? BloodGlucose { get; private set; }
    public GlucoseUnit GlucoseUnit { get; private set; }
    public short? PainLevel { get; private set; }
    public decimal? Weight { get; private set; }
    public WeightUnit WeightUnit { get; private set; }
    [SensitiveData] public string? Description { get; private set; }

    public override string AuditEntityType => "HEALTH_MEASUREMENT";

    public void Update(MeasurementData data, Guid userId)
    {
        Apply(data);
        TouchBy(userId);
    }

    /// <summary>
    /// Retorna os valores das métricas que possuem limite configurável, já em unidade canônica,
    /// para avaliação contra <see cref="HealthThreshold"/>.
    /// </summary>
    public IEnumerable<(HealthMetric Metric, decimal Value)> GetCanonicalValues()
    {
        if (Systolic.HasValue) yield return (HealthMetric.SystolicPressure, Systolic.Value);
        if (Diastolic.HasValue) yield return (HealthMetric.DiastolicPressure, Diastolic.Value);
        if (HeartRate.HasValue) yield return (HealthMetric.HeartRate, HeartRate.Value);
        if (Temperature.HasValue) yield return (HealthMetric.Temperature, UnitConverter.ToCelsius(Temperature.Value, TemperatureUnit));
        if (RespiratoryRate.HasValue) yield return (HealthMetric.RespiratoryRate, RespiratoryRate.Value);
        if (OxygenSaturation.HasValue) yield return (HealthMetric.OxygenSaturation, OxygenSaturation.Value);
        if (BloodGlucose.HasValue) yield return (HealthMetric.BloodGlucose, UnitConverter.ToMgPerDl(BloodGlucose.Value, GlucoseUnit));
    }

    private void Apply(MeasurementData d)
    {
        var hasAny = d.Systolic.HasValue || d.Diastolic.HasValue || d.HeartRate.HasValue || d.Temperature.HasValue
                     || d.RespiratoryRate.HasValue || d.OxygenSaturation.HasValue || d.BloodGlucose.HasValue
                     || d.PainLevel.HasValue || d.Weight.HasValue;
        if (!hasAny)
            throw new BusinessRuleException("MEASUREMENT_EMPTY", "Informe ao menos uma métrica de saúde.");
        if (d.Systolic.HasValue != d.Diastolic.HasValue)
            throw new BusinessRuleException("BLOOD_PRESSURE_INCOMPLETE", "Informe a pressão sistólica e a diastólica.");
        if (d.Systolic.HasValue && d.Systolic <= d.Diastolic)
            throw new BusinessRuleException("BLOOD_PRESSURE_INVALID", "A pressão sistólica deve ser maior que a diastólica.");

        MeasuredAt = d.MeasuredAt;
        Systolic = d.Systolic;
        Diastolic = d.Diastolic;
        HeartRate = d.HeartRate;
        Temperature = d.Temperature;
        TemperatureUnit = d.TemperatureUnit;
        RespiratoryRate = d.RespiratoryRate;
        OxygenSaturation = d.OxygenSaturation;
        BloodGlucose = d.BloodGlucose;
        GlucoseUnit = d.GlucoseUnit;
        PainLevel = d.PainLevel;
        Weight = d.Weight;
        WeightUnit = d.WeightUnit;
        Description = d.Description?.Trim();
    }
}

/// <summary>Dados de uma aferição.</summary>
public sealed record MeasurementData(
    DateTimeOffset MeasuredAt,
    short? Systolic,
    short? Diastolic,
    short? HeartRate,
    decimal? Temperature,
    TemperatureUnit TemperatureUnit,
    short? RespiratoryRate,
    short? OxygenSaturation,
    decimal? BloodGlucose,
    GlucoseUnit GlucoseUnit,
    short? PainLevel,
    decimal? Weight,
    WeightUnit WeightUnit,
    string? Description);
