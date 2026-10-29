namespace Lacos.Domain.Enums;

/// <summary>
/// Resultado do registro de uma dose (US02 "medicamentos ministrados"; RF12 "se a medicação não foi administrada").
/// </summary>
public enum MedicationAdministrationStatus
{
    /// <summary>Dose administrada.</summary>
    Administered,
    /// <summary>Dose não administrada (recusa, indisponibilidade etc.), registrada explicitamente pelo cuidador.</summary>
    NotAdministered
}
