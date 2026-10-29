using Lacos.Domain.Enums;

namespace Lacos.Domain.ValueObjects;

/// <summary>
/// Conversões de unidade de medida.
/// Motivo: RF09 determina que as métricas sejam registradas "de acordo com as unidades de medida
/// estabelecidas pelo paciente", enquanto os limites de <c>health_thresholds</c> são definidos em
/// unidades canônicas (°C, mg/dL, kg). Para comparar valor x limite, o valor é convertido para a unidade canônica.
/// </summary>
public static class UnitConverter
{
    /// <summary>Fator oficial de conversão de glicose: 1 mmol/L = 18,0182 mg/dL.</summary>
    public const decimal GlucoseMgPerDlPerMmol = 18.0182m;

    /// <summary>1 libra = 0,45359237 kg (definição internacional).</summary>
    public const decimal KilogramsPerPound = 0.45359237m;

    /// <summary>Converte temperatura para Celsius: °C = (°F − 32) × 5/9.</summary>
    public static decimal ToCelsius(decimal value, TemperatureUnit unit) =>
        unit == TemperatureUnit.Celsius ? value : Math.Round((value - 32m) * 5m / 9m, 1);

    /// <summary>Converte glicemia para mg/dL.</summary>
    public static decimal ToMgPerDl(decimal value, GlucoseUnit unit) =>
        unit == GlucoseUnit.MgPerDl ? value : Math.Round(value * GlucoseMgPerDlPerMmol, 2);

    /// <summary>Converte peso para quilogramas.</summary>
    public static decimal ToKilograms(decimal value, WeightUnit unit) =>
        unit == WeightUnit.Kilogram ? value : Math.Round(value * KilogramsPerPound, 2);
}
