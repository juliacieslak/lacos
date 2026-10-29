using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Limites clínicos configurados por paciente (tabela <c>health_thresholds</c>) — RF13.
///
/// Regras de negócio:
/// - Há uma única configuração por paciente (patient_id UNIQUE, conforme recomendação do modelo de dados).
/// - Cada métrica tem limite mínimo e máximo e uma flag "notice" que indica se aquele limite gera alerta.
/// - Semântica do alerta (descrição das colunas *_notice): o alerta é gerado quando o valor
///   ATINGE ou ULTRAPASSA o limite, ou seja: valor &lt;= mínimo ou valor &gt;= máximo.
/// - Unidades canônicas: mmHg, bpm, °C, respirações/min, %, mg/dL.
/// </summary>
public class HealthThreshold : BaseEntity, IAuditable
{
    private HealthThreshold() { }

    public HealthThreshold(Guid patientId, Guid createdByUserId)
    {
        PatientId = patientId;
        CreatedByUserId = createdByUserId;
    }

    public Guid PatientId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public short? SystolicMinLimit { get; private set; }
    public bool SystolicMinNotice { get; private set; }
    public short? SystolicMaxLimit { get; private set; }
    public bool SystolicMaxNotice { get; private set; }

    public short? DiastolicMinLimit { get; private set; }
    public bool DiastolicMinNotice { get; private set; }
    public short? DiastolicMaxLimit { get; private set; }
    public bool DiastolicMaxNotice { get; private set; }

    public short? HeartBpmMinLimit { get; private set; }
    public bool HeartBpmMinNotice { get; private set; }
    public short? HeartBpmMaxLimit { get; private set; }
    public bool HeartBpmMaxNotice { get; private set; }

    public decimal? TemperatureMinLimit { get; private set; }
    public bool TemperatureMinNotice { get; private set; }
    public decimal? TemperatureMaxLimit { get; private set; }
    public bool TemperatureMaxNotice { get; private set; }

    public short? RespiratoryRateMinLimit { get; private set; }
    public bool RespiratoryRateMinNotice { get; private set; }
    public short? RespiratoryRateMaxLimit { get; private set; }
    public bool RespiratoryRateMaxNotice { get; private set; }

    public short? OxygenSaturationMinLimit { get; private set; }
    public bool OxygenSaturationMinNotice { get; private set; }
    public short? OxygenSaturationMaxLimit { get; private set; }
    public bool OxygenSaturationMaxNotice { get; private set; }

    public decimal? BloodGlucoseMinLimit { get; private set; }
    public bool BloodGlucoseMinNotice { get; private set; }
    public decimal? BloodGlucoseMaxLimit { get; private set; }
    public bool BloodGlucoseMaxNotice { get; private set; }

    public string AuditEntityType => "HEALTH_THRESHOLD";
    public Guid? AuditPatientId => PatientId;

    /// <summary>
    /// Aplica a configuração de uma métrica. Valida coerência: mínimo &lt; máximo e
    /// que só é possível pedir alerta para um limite efetivamente informado.
    /// </summary>
    public void Configure(HealthMetric metric, MetricLimit limit, Guid userId)
    {
        limit.Validate(metric);
        UpdatedByUserId = userId;
        switch (metric)
        {
            case HealthMetric.SystolicPressure:
                (SystolicMinLimit, SystolicMinNotice, SystolicMaxLimit, SystolicMaxNotice) = limit.AsShort(); break;
            case HealthMetric.DiastolicPressure:
                (DiastolicMinLimit, DiastolicMinNotice, DiastolicMaxLimit, DiastolicMaxNotice) = limit.AsShort(); break;
            case HealthMetric.HeartRate:
                (HeartBpmMinLimit, HeartBpmMinNotice, HeartBpmMaxLimit, HeartBpmMaxNotice) = limit.AsShort(); break;
            case HealthMetric.RespiratoryRate:
                (RespiratoryRateMinLimit, RespiratoryRateMinNotice, RespiratoryRateMaxLimit, RespiratoryRateMaxNotice) = limit.AsShort(); break;
            case HealthMetric.OxygenSaturation:
                (OxygenSaturationMinLimit, OxygenSaturationMinNotice, OxygenSaturationMaxLimit, OxygenSaturationMaxNotice) = limit.AsShort(); break;
            case HealthMetric.Temperature:
                (TemperatureMinLimit, TemperatureMinNotice, TemperatureMaxLimit, TemperatureMaxNotice) =
                    (limit.Min, limit.MinNotice, limit.Max, limit.MaxNotice); break;
            case HealthMetric.BloodGlucose:
                (BloodGlucoseMinLimit, BloodGlucoseMinNotice, BloodGlucoseMaxLimit, BloodGlucoseMaxNotice) =
                    (limit.Min, limit.MinNotice, limit.Max, limit.MaxNotice); break;
            default: throw new ArgumentOutOfRangeException(nameof(metric));
        }
    }

    /// <summary>Retorna a configuração atual de uma métrica.</summary>
    public MetricLimit GetLimit(HealthMetric metric) => metric switch
    {
        HealthMetric.SystolicPressure => new(SystolicMinLimit, SystolicMinNotice, SystolicMaxLimit, SystolicMaxNotice),
        HealthMetric.DiastolicPressure => new(DiastolicMinLimit, DiastolicMinNotice, DiastolicMaxLimit, DiastolicMaxNotice),
        HealthMetric.HeartRate => new(HeartBpmMinLimit, HeartBpmMinNotice, HeartBpmMaxLimit, HeartBpmMaxNotice),
        HealthMetric.Temperature => new(TemperatureMinLimit, TemperatureMinNotice, TemperatureMaxLimit, TemperatureMaxNotice),
        HealthMetric.RespiratoryRate => new(RespiratoryRateMinLimit, RespiratoryRateMinNotice, RespiratoryRateMaxLimit, RespiratoryRateMaxNotice),
        HealthMetric.OxygenSaturation => new(OxygenSaturationMinLimit, OxygenSaturationMinNotice, OxygenSaturationMaxLimit, OxygenSaturationMaxNotice),
        HealthMetric.BloodGlucose => new(BloodGlucoseMinLimit, BloodGlucoseMinNotice, BloodGlucoseMaxLimit, BloodGlucoseMaxNotice),
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };

    /// <summary>
    /// Avalia um valor (já convertido para a unidade canônica) contra os limites da métrica.
    /// Algoritmo: se o limite mínimo existe, o alerta está habilitado e valor &lt;= mínimo → violação inferior;
    /// se o limite máximo existe, o alerta está habilitado e valor &gt;= máximo → violação superior.
    /// Retorna null quando não há alerta a gerar.
    /// </summary>
    public ThresholdBreach? Evaluate(HealthMetric metric, decimal value)
    {
        var limit = GetLimit(metric);
        if (limit.MinNotice && limit.Min.HasValue && value <= limit.Min.Value)
            return new ThresholdBreach(metric, value, limit.Min.Value, IsAboveMax: false);
        if (limit.MaxNotice && limit.Max.HasValue && value >= limit.Max.Value)
            return new ThresholdBreach(metric, value, limit.Max.Value, IsAboveMax: true);
        return null;
    }
}

/// <summary>Configuração de limite de uma métrica.</summary>
public sealed record MetricLimit(decimal? Min, bool MinNotice, decimal? Max, bool MaxNotice)
{
    /// <summary>Valida a coerência da configuração.</summary>
    public void Validate(HealthMetric metric)
    {
        if (Min.HasValue && Max.HasValue && Min.Value >= Max.Value)
            throw new BusinessRuleException("INVALID_THRESHOLD_RANGE", $"O limite mínimo de {metric} deve ser menor que o máximo.");
        if (MinNotice && !Min.HasValue)
            throw new BusinessRuleException("THRESHOLD_NOTICE_WITHOUT_LIMIT", $"Informe o limite mínimo de {metric} para habilitar o alerta.");
        if (MaxNotice && !Max.HasValue)
            throw new BusinessRuleException("THRESHOLD_NOTICE_WITHOUT_LIMIT", $"Informe o limite máximo de {metric} para habilitar o alerta.");
    }

    internal (short?, bool, short?, bool) AsShort() =>
        ((short?)Min, MinNotice, (short?)Max, MaxNotice);
}

/// <summary>Resultado de uma violação de limite.</summary>
public sealed record ThresholdBreach(HealthMetric Metric, decimal Value, decimal Limit, bool IsAboveMax);
