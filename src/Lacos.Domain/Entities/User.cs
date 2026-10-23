using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Usuário da aplicação (tabela <c>users</c>).
///
/// Origem das colunas:
/// - Modelo de dados (CSV): id, auth0_user_id, name, email, phone, profile_type, profile_completed, is_active, created_at, updated_at.
/// - RF01 (cadastro): CPF, data de nascimento, profissão, CEP, etnia e gênero — acrescentados ao modelo pois o requisito os exige.
/// - RF17: <see cref="PublicCode"/> iniciado pela letra "U".
///
/// A senha NÃO é armazenada: a autenticação (RF02) é delegada ao Auth0, conforme o fluxo de navegação da Sprint 3.
/// </summary>
public class User : BaseEntity, IAuditable
{
    private User() { } // Construtor exigido pelo EF Core.

    /// <summary>
    /// Registra um usuário a partir da identidade autenticada no Auth0 (primeiro login).
    /// O cadastro completo (RF01) é concluído no onboarding.
    /// </summary>
    public User(string auth0UserId, string email, string name, string publicCode)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId)) throw new ArgumentException("auth0_user_id obrigatório.", nameof(auth0UserId));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("E-mail obrigatório.", nameof(email));
        Auth0UserId = auth0UserId;
        Email = email.Trim().ToLowerInvariant();
        Name = string.IsNullOrWhiteSpace(name) ? Email : name.Trim();
        PublicCode = publicCode;
        IsActive = true;
        ProfileCompleted = false;
    }

    /// <summary>Identificador legível iniciado por "U" (RF17), ex.: U000123.</summary>
    public string PublicCode { get; private set; } = string.Empty;
    public string Auth0UserId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public ProfileType? ProfileType { get; private set; }
    public bool ProfileCompleted { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>CPF (somente dígitos), armazenado criptografado (RNF02).</summary>
    [SensitiveData]
    public string? Cpf { get; private set; }

    /// <summary>Blind index (HMAC-SHA256) do CPF, usado para garantir unicidade sem expor o valor.</summary>
    public string? CpfHash { get; private set; }

    public DateOnly? BirthDate { get; private set; }
    public string? Profession { get; private set; }
    public string? Cep { get; private set; }
    public Ethnicity? Ethnicity { get; private set; }
    public Gender? Gender { get; private set; }

    /// <summary>Preenchido quando o usuário solicita exclusão definitiva (LGPD) e seus dados são anonimizados.</summary>
    public DateTimeOffset? AnonymizedAt { get; private set; }

    public string AuditEntityType => "USER";
    public Guid? AuditPatientId => null;

    /// <summary>
    /// Conclui o onboarding com os dados obrigatórios do RF01.
    /// Regra: um onboarding só pode ser concluído uma vez; alterações posteriores usam <see cref="UpdateProfile"/>.
    /// </summary>
    public void CompleteOnboarding(ProfileType profileType, string name, string cpf, string cpfHash, DateOnly birthDate,
        string phone, string profession, string cep, Ethnicity ethnicity, Gender gender)
    {
        EnsureActive();
        if (ProfileCompleted)
            throw new BusinessRuleException("ONBOARDING_ALREADY_COMPLETED", "O cadastro inicial já foi concluído.");

        ProfileType = profileType;
        Cpf = cpf;
        CpfHash = cpfHash;
        ApplyProfile(name, birthDate, phone, profession, cep, ethnicity, gender);
        ProfileCompleted = true;
    }

    /// <summary>Atualiza os dados cadastrais (exceto CPF, que é imutável após o onboarding, e e-mail, gerido pelo Auth0).</summary>
    public void UpdateProfile(string name, DateOnly birthDate, string phone, string profession, string cep,
        Ethnicity ethnicity, Gender gender, ProfileType profileType)
    {
        EnsureActive();
        if (!ProfileCompleted)
            throw new BusinessRuleException("ONBOARDING_REQUIRED", "Conclua o cadastro inicial antes de editar o perfil.");
        ProfileType = profileType;
        ApplyProfile(name, birthDate, phone, profession, cep, ethnicity, gender);
    }

    /// <summary>Sincroniza o e-mail com o provedor de identidade (fonte da verdade do e-mail).</summary>
    public void SyncEmail(string email)
    {
        if (!string.IsNullOrWhiteSpace(email)) Email = email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Exclusão definitiva dos dados pessoais (LGPD - RNF01).
    /// O registro é mantido apenas como "lápide" anonimizada para preservar a integridade referencial
    /// da trilha de auditoria imutável (RNF04); todos os dados pessoais são apagados.
    /// </summary>
    public void Anonymize(DateTimeOffset when)
    {
        Name = "Usuário removido";
        Email = $"removed+{Id:N}@lacos.invalid";
        Auth0UserId = $"removed|{Id:N}";
        Phone = null;
        Cpf = null;
        CpfHash = null;
        BirthDate = null;
        Profession = null;
        Cep = null;
        Ethnicity = null;
        Gender = null;
        IsActive = false;
        AnonymizedAt = when;
    }

    private void ApplyProfile(string name, DateOnly birthDate, string phone, string profession, string cep,
        Ethnicity ethnicity, Gender gender)
    {
        Name = name.Trim();
        BirthDate = birthDate;
        Phone = phone.Trim();
        Profession = profession.Trim();
        Cep = cep;
        Ethnicity = ethnicity;
        Gender = gender;
    }

    private void EnsureActive()
    {
        if (!IsActive) throw new ForbiddenException("Usuário inativo.", "USER_INACTIVE");
    }
}
