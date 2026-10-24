namespace Lacos.Domain.Enums;

/// <summary>Unidade de temperatura corporal (RF09: "de acordo com as unidades de medida estabelecidas pelo paciente").</summary>
public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

/// <summary>Unidade de glicemia. Padrão sugerido pelo modelo de dados: mg/dL.</summary>
public enum GlucoseUnit
{
    MgPerDl,
    MmolPerL
}

/// <summary>Unidade de peso.</summary>
public enum WeightUnit
{
    Kilogram,
    Pound
}
