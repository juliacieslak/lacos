namespace Lacos.Domain.Enums;

/// <summary>Métricas de saúde que possuem limites configuráveis em <c>health_thresholds</c> (RF13).</summary>
public enum HealthMetric
{
    SystolicPressure,
    DiastolicPressure,
    HeartRate,
    Temperature,
    RespiratoryRate,
    OxygenSaturation,
    BloodGlucose
}
