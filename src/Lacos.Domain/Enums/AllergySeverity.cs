namespace Lacos.Domain.Enums;

/// <summary>
/// Nível de gravidade da alergia (RF16).
/// SUPOSIÇÃO: escala de quatro níveis usada em prontuários (leve, moderada, grave, risco de vida).
/// </summary>
public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe,
    LifeThreatening
}
