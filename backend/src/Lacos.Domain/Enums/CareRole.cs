namespace Lacos.Domain.Enums;

/// <summary>
/// Papel específico do usuário no cuidado de um paciente (patient_user_permissions.role),
/// independente do <see cref="ProfileType"/> geral do usuário.
/// SUPOSIÇÃO: a documentação não enumera os papéis; foram derivados dos atores do diagrama de casos de uso.
/// </summary>
public enum CareRole
{
    /// <summary>Administrador do perfil do paciente (criador do perfil ou promovido por outro administrador).</summary>
    Administrator,
    /// <summary>Cuidador responsável pelo acompanhamento diário.</summary>
    Caregiver,
    /// <summary>Familiar integrante da rede de cuidado.</summary>
    FamilyMember,
    /// <summary>Profissional da saúde que acompanha o paciente.</summary>
    HealthProfessional
}
