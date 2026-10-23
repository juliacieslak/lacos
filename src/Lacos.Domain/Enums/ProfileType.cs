namespace Lacos.Domain.Enums;

/// <summary>
/// Tipo de perfil escolhido no onboarding (users.profile_type).
/// Valores previstos no modelo de dados: FAMILY, CAREGIVER ou HEALTH_PROFESSIONAL.
/// IMPORTANTE: o tipo de perfil NÃO concede acesso automático a nenhum paciente;
/// o acesso é controlado exclusivamente por <c>patient_user_permissions</c>.
/// </summary>
public enum ProfileType
{
    /// <summary>Familiar / responsável (ator "Administrador do Perfil" quando cria um paciente).</summary>
    Family,
    /// <summary>Cuidador (formal ou informal).</summary>
    Caregiver,
    /// <summary>Profissional da saúde (médico, enfermeiro, terapeuta).</summary>
    HealthProfessional
}
