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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Application.Services;

/// <summary>
/// Gestão da rede de cuidado do paciente: membros, permissões, convites e revogação
/// (UC04 Gerenciar Permissões de Acesso, UC05 Revogar Acesso ao Perfil, RF07, US01).
/// </summary>
public interface ICareTeamService
{
    Task<IReadOnlyList<CareTeamMemberResponse>> ListMembersAsync(Guid patientId, bool includeInactive, CancellationToken ct = default);
    Task<CareTeamMemberResponse> UpdateMemberAsync(Guid patientId, Guid permissionId, UpdateMemberRequest request, CancellationToken ct = default);
    Task RevokeMemberAsync(Guid patientId, Guid permissionId, RevokeMemberRequest request, CancellationToken ct = default);
    Task<InvitationResponse> InviteAsync(Guid patientId, InviteMemberRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationResponse>> ListInvitationsAsync(Guid patientId, CancellationToken ct = default);
    Task RevokeInvitationAsync(Guid patientId, Guid invitationId, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationResponse>> ListMyInvitationsAsync(CancellationToken ct = default);
    Task<CareTeamMemberResponse> AcceptInvitationAsync(Guid invitationId, CancellationToken ct = default);
    Task DeclineInvitationAsync(Guid invitationId, CancellationToken ct = default);
}

public sealed class CareTeamService : ICareTeamService
{
    private readonly IPatientAccessService _access;
    private readonly ICurrentUserContext _userContext;
    private readonly ICurrentUser _identity;
    private readonly IPatientPermissionRepository _permissions;
    private readonly IPatientInvitationRepository _invitations;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly IUserRepository _users;
    private readonly IEmailSender _email;
    private readonly IAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly InvitationOptions _options;
    private readonly ILogger<CareTeamService> _logger;

    public CareTeamService(IPatientAccessService access, ICurrentUserContext userContext, ICurrentUser identity,
        IPatientPermissionRepository permissions, IPatientInvitationRepository invitations, INotificationPreferenceRepository preferences,
        IUserRepository users, IEmailSender email, IAuditService audit, IUnitOfWork unitOfWork, IDateTimeProvider clock,
        IOptions<InvitationOptions> options, ILogger<CareTeamService> logger)
    {
        _access = access;
        _userContext = userContext;
        _identity = identity;
        _permissions = permissions;
        _invitations = invitations;
        _preferences = preferences;
        _users = users;
        _email = email;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Lista os membros ("Quem está cuidando" - WF01). Qualquer membro com permissão de consulta vê os ativos;
    /// apenas gestores podem ver também os vínculos revogados (histórico de acessos).
    /// </summary>
    public async Task<IReadOnlyList<CareTeamMemberResponse>> ListMembersAsync(Guid patientId, bool includeInactive, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.View, ct);
        if (includeInactive && !access.Permission.CanManageMembers)
            throw new ForbiddenException("Somente administradores podem consultar acessos revogados.");
        var members = await _permissions.ListByPatientAsync(patientId, includeInactive, ct);
        return members.Select(m => m.ToMemberResponse()).ToList();
    }

    /// <summary>
    /// Altera papel/permissões de um membro (UC04). Regras:
    /// - Exige permissão de gerenciar membros.
    /// - O paciente nunca pode ficar sem ao menos um gestor ativo.
    /// - O papel Administrador exige a permissão de gerenciar membros.
    /// </summary>
    public async Task<CareTeamMemberResponse> UpdateMemberAsync(Guid patientId, Guid permissionId, UpdateMemberRequest r, CancellationToken ct = default)
    {
        await _access.RequireAsync(patientId, PatientPermission.ManageMembers, ct);
        var member = await GetMemberAsync(patientId, permissionId, ct);
        var permissions = ResolvePermissions(r.Role, r.Preset, r.Permissions);

        if (member.CanManageMembers && !permissions.CanManageMembers)
            await EnsureNotLastManagerAsync(patientId, ct);

        member.Update(r.Role, r.RelationshipType, permissions);
        await _unitOfWork.SaveChangesAsync(ct);
        return member.ToMemberResponse();
    }

    /// <summary>
    /// Revoga o acesso de um membro (UC05) — efeito imediato (US01). Um membro também pode sair por conta própria.
    /// As preferências de notificação daquele paciente são desativadas.
    /// </summary>
    public async Task RevokeMemberAsync(Guid patientId, Guid permissionId, RevokeMemberRequest r, CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        var member = await GetMemberAsync(patientId, permissionId, ct);
        var isSelf = member.UserId == userId;
        if (!isSelf) await _access.RequireAsync(patientId, PatientPermission.ManageMembers, ct);
        if (!member.IsActive) return;
        if (member.CanManageMembers) await EnsureNotLastManagerAsync(patientId, ct);

        member.Revoke();
        var pref = await _preferences.GetAsync(patientId, member.UserId, ct);
        pref?.Deactivate();
        await _audit.RecordAsync(userId, patientId, "PATIENT_USER_PERMISSION", member.Id, AuditAction.StatusChange,
            new { member.UserId, IsActive = false }, r.Reason ?? (isSelf ? "Saída voluntária da rede de cuidado" : null), ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Convida um usuário por e-mail (US01). Regras:
    /// - Exige permissão de gerenciar membros.
    /// - Não é possível convidar quem já é membro ativo nem duplicar convite pendente.
    /// - O convite expira após o prazo configurado.
    /// - O e-mail é enviado após a gravação; falha no envio não desfaz o convite (o convidado também vê o convite ao entrar).
    /// </summary>
    public async Task<InvitationResponse> InviteAsync(Guid patientId, InviteMemberRequest r, CancellationToken ct = default)
    {
        var access = await _access.RequireAsync(patientId, PatientPermission.ManageMembers, ct);
        var email = r.Email.Trim().ToLowerInvariant();
        var now = _clock.UtcNow;

        var existingUser = await _users.GetByEmailAsync(email, ct);
        if (existingUser is not null)
        {
            var link = await _permissions.GetAsync(patientId, existingUser.Id, ct);
            if (link is { IsActive: true })
                throw new ConflictException("ALREADY_MEMBER", "Este usuário já faz parte da rede de cuidado.");
        }
        if (await _invitations.ExistsPendingAsync(patientId, email, now, ct))
            throw new ConflictException("INVITATION_ALREADY_PENDING", "Já existe um convite pendente para este e-mail.");

        var invitation = new PatientInvitation(patientId, email, access.UserId, r.Role, r.RelationshipType,
            ResolvePermissions(r.Role, r.Preset, r.Permissions), now.AddDays(_options.ExpirationDays));
        await _invitations.AddAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var inviter = await _userContext.GetRequiredUserAsync(ct);
        await TrySendInvitationEmailAsync(invitation, inviter.Name, access.Patient.FullName, ct);
        return invitation.ToResponse(inviter.Name) with { PatientName = access.Patient.FullName };
    }

    public async Task<IReadOnlyList<InvitationResponse>> ListInvitationsAsync(Guid patientId, CancellationToken ct = default)
    {
        await _access.RequireAsync(patientId, PatientPermission.ManageMembers, ct);
        var invitations = await _invitations.ListByPatientAsync(patientId, ct);
        foreach (var i in invitations) i.ExpireIfNeeded(_clock.UtcNow);
        var names = await _users.GetNamesAsync(invitations.Select(i => i.InvitedByUserId), ct);
        return invitations.Select(i => i.ToResponse(names.GetValueOrDefault(i.InvitedByUserId))).ToList();
    }

    public async Task RevokeInvitationAsync(Guid patientId, Guid invitationId, CancellationToken ct = default)
    {
        await _access.RequireAsync(patientId, PatientPermission.ManageMembers, ct);
        var invitation = await _invitations.GetByIdAsync(invitationId, ct);
        if (invitation is null || invitation.PatientId != patientId) throw new NotFoundException("Convite", invitationId);
        invitation.Revoke(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Convites pendentes para o e-mail verificado do usuário ("Tela de acesso pendente").</summary>
    public async Task<IReadOnlyList<InvitationResponse>> ListMyInvitationsAsync(CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        if (!_identity.EmailVerified) return Array.Empty<InvitationResponse>();
        var pending = await _invitations.ListPendingByEmailAsync(user.Email, _clock.UtcNow, ct);
        var names = await _users.GetNamesAsync(pending.Select(i => i.InvitedByUserId), ct);
        return pending.Select(i => i.ToResponse(names.GetValueOrDefault(i.InvitedByUserId))).ToList();
    }

    /// <summary>
    /// Aceita um convite. Regras de segurança: o e-mail do token precisa estar verificado e ser igual ao do convite
    /// (impede que alguém aceite convites de terceiros). Cria (ou reativa) o vínculo com as permissões do convite
    /// e preferências de notificação padrão.
    /// </summary>
    public async Task<CareTeamMemberResponse> AcceptInvitationAsync(Guid invitationId, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        if (!_identity.EmailVerified)
            throw new ForbiddenException("Confirme seu e-mail antes de aceitar convites.", "EMAIL_NOT_VERIFIED");
        if (!user.ProfileCompleted)
            throw new BusinessRuleException("ONBOARDING_REQUIRED", "Conclua seu cadastro antes de aceitar convites.");

        var invitation = await _invitations.GetByIdAsync(invitationId, ct) ?? throw new NotFoundException("Convite", invitationId);
        invitation.Accept(user.Id, user.Email, _clock.UtcNow);

        var link = await _permissions.GetAsync(invitation.PatientId, user.Id, ct);
        if (link is null)
        {
            link = new PatientUserPermission(invitation.PatientId, user.Id, invitation.Role, invitation.RelationshipType, invitation.Permissions);
            await _permissions.AddAsync(link, ct);
        }
        else
        {
            link.Reactivate(invitation.Role, invitation.RelationshipType, invitation.Permissions);
        }

        var pref = await _preferences.GetAsync(invitation.PatientId, user.Id, ct);
        if (pref is null) await _preferences.AddAsync(new NotificationPreference(invitation.PatientId, user.Id), ct);
        else pref.Activate();

        await _unitOfWork.SaveChangesAsync(ct);
        var members = await _permissions.ListByPatientAsync(invitation.PatientId, false, ct);
        return (members.FirstOrDefault(m => m.Id == link.Id) ?? link).ToMemberResponse();
    }

    public async Task DeclineInvitationAsync(Guid invitationId, CancellationToken ct = default)
    {
        var user = await _userContext.GetRequiredUserAsync(ct);
        if (!_identity.EmailVerified)
            throw new ForbiddenException("Confirme seu e-mail antes de responder convites.", "EMAIL_NOT_VERIFIED");
        var invitation = await _invitations.GetByIdAsync(invitationId, ct) ?? throw new NotFoundException("Convite", invitationId);
        invitation.Decline(user.Email, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Converte preset/permissões informadas em <see cref="PermissionSet"/>, validando coerência com o papel.</summary>
    private static PermissionSet ResolvePermissions(CareRole role, AccessProfilePreset preset, PermissionsDto? custom)
    {
        PermissionSet permissions;
        if (preset == AccessProfilePreset.Custom)
        {
            if (custom is null) throw new ValidationFailedException("permissions", "Informe as permissões para o perfil personalizado.");
            permissions = custom.ToDomain().Normalize();
            if (!permissions.HasAny) throw new ValidationFailedException("permissions", "Conceda ao menos a permissão de consulta.");
        }
        else permissions = PermissionSet.FromPreset(preset);

        if (role == CareRole.Administrator && !permissions.CanManageMembers)
            throw new BusinessRuleException("INVALID_ROLE_PERMISSIONS", "O papel Administrador exige a permissão de gerenciar membros.");
        return permissions;
    }

    private async Task<PatientUserPermission> GetMemberAsync(Guid patientId, Guid permissionId, CancellationToken ct)
    {
        var member = await _permissions.GetByIdAsync(permissionId, ct);
        if (member is null || member.PatientId != patientId) throw new NotFoundException("Membro", permissionId);
        return member;
    }

    private async Task EnsureNotLastManagerAsync(Guid patientId, CancellationToken ct)
    {
        if (await _permissions.CountActiveManagersAsync(patientId, ct) <= 1)
            throw new BusinessRuleException("LAST_MANAGER", "O paciente precisa de ao menos um administrador com permissão de gerenciar membros.");
    }

    private async Task TrySendInvitationEmailAsync(PatientInvitation invitation, string inviterName, string patientName, CancellationToken ct)
    {
        try
        {
            var body = $"""
                <p>Olá!</p>
                <p><strong>{System.Net.WebUtility.HtmlEncode(inviterName)}</strong> convidou você para participar da rede de cuidado de
                <strong>{System.Net.WebUtility.HtmlEncode(patientName)}</strong> no Laços.</p>
                <p>Acesse <a href="{_options.AcceptUrl}">{_options.AcceptUrl}</a> com este e-mail para aceitar.
                O convite expira em {invitation.ExpiresAt:dd/MM/yyyy}.</p>
                """;
            await _email.SendAsync(invitation.InvitedEmail, invitation.InvitedEmail, "Convite para a rede de cuidado — Laços", body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail do convite {InvitationId}", invitation.Id);
        }
    }
}
