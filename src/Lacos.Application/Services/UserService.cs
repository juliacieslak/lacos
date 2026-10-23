using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Application.Mappings;
using Lacos.Application.Options;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;
using Lacos.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Application.Services;

/// <summary>
/// Casos de uso do usuário: início de sessão (UC02), cadastro/onboarding (UC01 / RF01), edição de perfil,
/// consentimentos LGPD e exclusão definitiva da conta (RNF01).
/// </summary>
public interface IUserService
{
    Task<SessionResponse> StartSessionAsync(int sessionTimeoutMinutes, CancellationToken ct = default);
    Task<UserResponse> GetMeAsync(CancellationToken ct = default);
    Task<UserResponse> CompleteOnboardingAsync(CompleteOnboardingRequest request, CancellationToken ct = default);
    Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<ConsentStatusResponse> GetConsentsAsync(CancellationToken ct = default);
    Task<ConsentStatusResponse> AcceptConsentsAsync(AcceptConsentsRequest request, CancellationToken ct = default);
    Task<ConsentStatusResponse> RevokeConsentAsync(ConsentType type, CancellationToken ct = default);
    Task<bool> HasCurrentConsentsAsync(Guid userId, CancellationToken ct = default);
    Task DeleteAccountAsync(DeleteAccountRequest request, CancellationToken ct = default);
}

public sealed class UserService : IUserService
{
    private readonly ICurrentUser _identity;
    private readonly ICurrentUserContext _userContext;
    private readonly IUserRepository _users;
    private readonly IUserConsentRepository _consents;
    private readonly IPatientPermissionRepository _permissions;
    private readonly IPatientInvitationRepository _invitations;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly INotificationRepository _notifications;
    private readonly IPublicCodeGenerator _codes;
    private readonly IBlindIndexService _blindIndex;
    private readonly IIdentityProviderService _identityProvider;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly LgpdOptions _lgpd;
    private readonly ILogger<UserService> _logger;

    public UserService(ICurrentUser identity, ICurrentUserContext userContext, IUserRepository users, IUserConsentRepository consents,
        IPatientPermissionRepository permissions, IPatientInvitationRepository invitations, INotificationPreferenceRepository preferences,
        INotificationRepository notifications, IPublicCodeGenerator codes, IBlindIndexService blindIndex,
        IIdentityProviderService identityProvider, IAuditService audit, IUnitOfWork unitOfWork, IDateTimeProvider clock,
        IOptions<LgpdOptions> lgpd, ILogger<UserService> logger)
    {
        _identity = identity;
        _userContext = userContext;
        _users = users;
        _consents = consents;
        _permissions = permissions;
        _invitations = invitations;
        _preferences = preferences;
        _notifications = notifications;
        _codes = codes;
        _blindIndex = blindIndex;
        _identityProvider = identityProvider;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lgpd = lgpd.Value;
        _logger = logger;
    }

    /// <summary>
    /// Início de sessão após autenticação no Auth0 (UC02 / fluxo "Autenticação Auth0 → Verificação de acesso").
    /// Fluxo:
    /// 1. Localiza o usuário pelo "sub"; se não existir, registra-o (primeiro acesso) com código "U..." (RF17).
    /// 2. Sincroniza o e-mail (fonte da verdade = provedor de identidade).
    /// 3. Calcula a situação de acesso: Autorizado / Aguardando aprovação / Sem convite.
    /// </summary>
    public async Task<SessionResponse> StartSessionAsync(int sessionTimeoutMinutes, CancellationToken ct = default)
    {
        if (!_identity.IsAuthenticated || string.IsNullOrEmpty(_identity.Auth0UserId))
            throw new ForbiddenException("Usuário não autenticado.", "UNAUTHENTICATED");

        var user = await _users.GetByAuth0IdAsync(_identity.Auth0UserId, ct);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(_identity.Email))
                throw new BusinessRuleException("EMAIL_CLAIM_MISSING", "O token não contém o e-mail do usuário. Verifique a configuração do Auth0.");
            if (await _users.GetByEmailAsync(_identity.Email, ct) is not null)
                throw new ConflictException("EMAIL_ALREADY_REGISTERED", "Já existe uma conta com este e-mail vinculada a outro método de login.");

            user = new User(_identity.Auth0UserId, _identity.Email, _identity.Name ?? _identity.Email, await _codes.NextUserCodeAsync(ct));
            await _users.AddAsync(user, ct);
            _logger.LogInformation("Novo usuário registrado {UserId} ({PublicCode})", user.Id, user.PublicCode);
        }
        else
        {
            if (!user.IsActive) throw new ForbiddenException("Usuário inativo.", "USER_INACTIVE");
            if (_identity.EmailVerified && !string.IsNullOrWhiteSpace(_identity.Email)) user.SyncEmail(_identity.Email);
        }
        await _unitOfWork.SaveChangesAsync(ct);

        var activeLinks = await _permissions.ListActiveByUserAsync(user.Id, ct);
        var pending = _identity.EmailVerified
            ? await _invitations.ListPendingByEmailAsync(user.Email, _clock.UtcNow, ct)
            : Array.Empty<PatientInvitation>();

        var status = activeLinks.Count > 0 ? AccessStatus.Authorized
            : pending.Count > 0 ? AccessStatus.PendingApproval
            : AccessStatus.NoInvitation;

        var inviterNames = await _users.GetNamesAsync(pending.Select(i => i.InvitedByUserId), ct);
        return new SessionResponse(user.ToResponse(), status, await HasCurrentConsentsAsync(user.Id, ct),
            pending.Select(i => i.ToResponse(inviterNames.GetValueOrDefault(i.InvitedByUserId))).ToList(), sessionTimeoutMinutes);
    }

    public async Task<UserResponse> GetMeAsync(CancellationToken ct = default) =>
        (await _userContext.GetRequiredUserAsync(ct)).ToResponse();

    /// <summary>
    /// Conclui o cadastro (RF01). Regras:
    /// - CPF válido e único entre usuários (verificado via blind index, sem expor o CPF).
    /// - Aceite obrigatório dos termos de uso e do tratamento de dados sensíveis (RNF01).
    /// - Gera auditoria PROFILE_COMPLETED (ação prevista no modelo de dados).
    /// </summary>
    public async Task<UserResponse> CompleteOnboardingAsync(CompleteOnboardingRequest r, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        if (!r.AcceptTermsOfUse || !r.AcceptSensitiveDataProcessing)
            throw new BusinessRuleException("CONSENT_REQUIRED", "É necessário aceitar os termos de uso e o tratamento de dados sensíveis de saúde.");

        var cpf = Cpf.Create(r.Cpf);
        var cpfHash = _blindIndex.Compute(cpf.Value);
        if (await _users.ExistsByCpfHashAsync(cpfHash, user.Id, ct))
            throw new ConflictException("CPF_ALREADY_REGISTERED", "Já existe um usuário cadastrado com este CPF.");

        user.CompleteOnboarding(r.ProfileType, r.Name, cpf.Value, cpfHash, r.BirthDate, r.Phone, r.Profession,
            Cep.Create(r.Cep).Value, r.Ethnicity, r.Gender);

        await RegisterConsentsAsync(user.Id, terms: true, sensitive: true, ct);
        await _audit.RecordAsync(user.Id, null, "USER", user.Id, AuditAction.ProfileCompleted,
            new { user.ProfileType, user.ProfileCompleted }, ct: ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest r, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        user.UpdateProfile(r.Name, r.BirthDate, r.Phone, r.Profession, Cep.Create(r.Cep).Value, r.Ethnicity, r.Gender, r.ProfileType);
        await _unitOfWork.SaveChangesAsync(ct);
        return user.ToResponse();
    }

    public async Task<ConsentStatusResponse> GetConsentsAsync(CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        return await BuildConsentStatusAsync(user.Id, ct);
    }

    /// <summary>Registra novo aceite (ex.: após atualização da versão dos termos).</summary>
    public async Task<ConsentStatusResponse> AcceptConsentsAsync(AcceptConsentsRequest r, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        await RegisterConsentsAsync(user.Id, r.AcceptTermsOfUse, r.AcceptSensitiveDataProcessing, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return await BuildConsentStatusAsync(user.Id, ct);
    }

    /// <summary>
    /// Revoga um consentimento (direito do titular - LGPD). Sem consentimento vigente, o acesso aos dados
    /// de pacientes é bloqueado pelo filtro de consentimento da API até novo aceite.
    /// </summary>
    public async Task<ConsentStatusResponse> RevokeConsentAsync(ConsentType type, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        foreach (var consent in (await _consents.ListByUserAsync(user.Id, ct)).Where(c => c.ConsentType == type && c.RevokedAt is null))
            consent.Revoke(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
        return await BuildConsentStatusAsync(user.Id, ct);
    }

    /// <summary>Verifica se o usuário aceitou as versões vigentes dos dois documentos.</summary>
    public async Task<bool> HasCurrentConsentsAsync(Guid userId, CancellationToken ct = default)
    {
        var consents = await _consents.ListByUserAsync(userId, ct);
        return consents.Any(c => c.ConsentType == ConsentType.TermsOfUse && c.IsValidFor(_lgpd.TermsOfUseVersion))
               && consents.Any(c => c.ConsentType == ConsentType.SensitiveHealthData && c.IsValidFor(_lgpd.SensitiveDataConsentVersion));
    }

    /// <summary>
    /// Exclusão definitiva da conta (RNF01). Regras:
    /// 1. Confirmação explícita ("EXCLUIR").
    /// 2. Se o usuário for o único gestor de um paciente, a exclusão é bloqueada até transferir a gestão a outro
    ///    membro ou excluir o paciente — evita pacientes sem administrador (SUPOSIÇÃO documentada).
    /// 3. Revoga vínculos, desativa preferências, apaga notificações, revoga consentimentos e anonimiza os dados pessoais.
    /// 4. Remove a conta no Auth0. A trilha de auditoria é mantida (obrigação legal) referenciando o usuário anonimizado.
    /// </summary>
    public async Task DeleteAccountAsync(DeleteAccountRequest r, CancellationToken ct = default)
    {
        if (!string.Equals(r.Confirmation?.Trim(), "EXCLUIR", StringComparison.Ordinal))
            throw new ValidationFailedException(nameof(r.Confirmation), "Digite EXCLUIR para confirmar a exclusão definitiva.");

        var user = await _userContext.GetRequiredUserAsync(ct);
        var links = await _permissions.ListActiveByUserAsync(user.Id, ct);
        foreach (var link in links.Where(l => l.CanManageMembers))
        {
            if (await _permissions.CountActiveManagersAsync(link.PatientId, ct) <= 1)
                throw new ConflictException("LAST_MANAGER",
                    $"Você é o único administrador do paciente {link.Patient?.PublicCode}. Transfira a gestão ou exclua o paciente antes de excluir sua conta.");
        }

        foreach (var link in links) link.Revoke();
        foreach (var pref in await _preferences.ListByUserAsync(user.Id, ct)) pref.Deactivate();
        foreach (var consent in await _consents.ListByUserAsync(user.Id, ct)) consent.Revoke(_clock.UtcNow);
        await _notifications.DeleteByUserAsync(user.Id, ct);

        var auth0Id = user.Auth0UserId;
        await _audit.RecordAsync(user.Id, null, "USER", user.Id, AuditAction.Erasure, reason: r.Reason, ct: ct);
        user.Anonymize(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);

        // Executado após gravar: se o provedor falhar, os dados locais já estão anonimizados e a falha é registrada
        // para tratamento operacional (a conta local já não permite acesso, pois o usuário está inativo).
        try { await _identityProvider.DeleteUserAsync(auth0Id, ct); }
        catch (Exception ex) { _logger.LogError(ex, "Falha ao remover usuário {UserId} do provedor de identidade", user.Id); }
    }

    private async Task RegisterConsentsAsync(Guid userId, bool terms, bool sensitive, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        if (terms) await _consents.AddAsync(new UserConsent(userId, ConsentType.TermsOfUse, _lgpd.TermsOfUseVersion, now, _identity.IpAddress), ct);
        if (sensitive) await _consents.AddAsync(new UserConsent(userId, ConsentType.SensitiveHealthData, _lgpd.SensitiveDataConsentVersion, now, _identity.IpAddress), ct);
    }

    private async Task<ConsentStatusResponse> BuildConsentStatusAsync(Guid userId, CancellationToken ct)
    {
        var consents = await _consents.ListByUserAsync(userId, ct);
        var items = consents.OrderByDescending(c => c.AcceptedAt).Select(c => new ConsentResponse(c.ConsentType, c.Version, c.AcceptedAt, c.RevokedAt,
            c.IsValidFor(c.ConsentType == ConsentType.TermsOfUse ? _lgpd.TermsOfUseVersion : _lgpd.SensitiveDataConsentVersion))).ToList();
        return new ConsentStatusResponse(_lgpd.TermsOfUseVersion, _lgpd.SensitiveDataConsentVersion,
            await HasCurrentConsentsAsync(userId, ct), items);
    }
}
