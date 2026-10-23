using Lacos.Application.Common;
using Lacos.Domain.Enums;

namespace Lacos.Application.DTOs;

public sealed record UserResponse(
    Guid Id, string PublicCode, string Name, string Email, string? Phone, ProfileType? ProfileType,
    bool ProfileCompleted, bool IsActive, string? CpfMasked, DateOnly? BirthDate, string? Profession,
    string? Cep, Ethnicity? Ethnicity, Gender? Gender, DateTimeOffset CreatedAt);

/// <summary>Resposta do início de sessão: dados do usuário + situação de acesso (fluxo de navegação da Sprint 3).</summary>
public sealed record SessionResponse(
    UserResponse User,
    AccessStatus AccessStatus,
    bool ConsentsUpToDate,
    IReadOnlyList<InvitationResponse> PendingInvitations,
    int SessionTimeoutMinutes);

/// <summary>
/// Conclusão do cadastro (RF01): nome completo, CPF, data de nascimento, telefone, profissão, CEP, etnia e gênero.
/// E-mail e senha são gerenciados pelo Auth0. O aceite LGPD é obrigatório (RNF01).
/// </summary>
public sealed record CompleteOnboardingRequest(
    ProfileType ProfileType, string Name, string Cpf, DateOnly BirthDate, string Phone, string Profession,
    string Cep, Ethnicity Ethnicity, Gender Gender, bool AcceptTermsOfUse, bool AcceptSensitiveDataProcessing);

public sealed record UpdateProfileRequest(
    ProfileType ProfileType, string Name, DateOnly BirthDate, string Phone, string Profession,
    string Cep, Ethnicity Ethnicity, Gender Gender);

public sealed record ConsentResponse(ConsentType ConsentType, string Version, DateTimeOffset AcceptedAt, DateTimeOffset? RevokedAt, bool IsCurrent);

public sealed record ConsentStatusResponse(
    string TermsOfUseVersion, string SensitiveDataConsentVersion, bool UpToDate, IReadOnlyList<ConsentResponse> Consents);

public sealed record AcceptConsentsRequest(bool AcceptTermsOfUse, bool AcceptSensitiveDataProcessing);

public sealed record RevokeConsentRequest(ConsentType ConsentType);

/// <summary>Exclusão definitiva da conta (LGPD). Exige digitar "EXCLUIR" como confirmação explícita.</summary>
public sealed record DeleteAccountRequest(string Confirmation, string? Reason);
