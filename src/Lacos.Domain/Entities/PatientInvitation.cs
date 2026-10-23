using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Convite para integrar a rede de cuidado de um paciente (tabela <c>patient_invitations</c>).
///
/// Origem: US01 ("Permite inserir o e-mail do usuário a ser convidado") e o fluxo "Verificação de acesso"
/// da Sprint 3 (aguardando aprovação / sem convite / acesso autorizado).
/// SUPOSIÇÕES documentadas:
/// - O convite é feito por e-mail e aceito pelo próprio convidado após login com e-mail verificado.
/// - "Aguardando aprovação" = existe convite pendente de aceite para o e-mail do usuário.
/// - Validade padrão de 7 dias (configurável).
/// </summary>
public class PatientInvitation : BaseEntity, IAuditable
{
    private PatientInvitation() { }

    public PatientInvitation(Guid patientId, string invitedEmail, Guid invitedByUserId, CareRole role,
        string relationshipType, PermissionSet permissions, DateTimeOffset expiresAt)
    {
        if (role == CareRole.Administrator && !permissions.CanManageMembers)
            throw new BusinessRuleException("INVALID_ROLE_PERMISSIONS", "O papel Administrador exige permissão de gerenciar membros.");
        PatientId = patientId;
        InvitedEmail = invitedEmail.Trim().ToLowerInvariant();
        InvitedByUserId = invitedByUserId;
        Role = role;
        RelationshipType = relationshipType.Trim();
        var p = permissions.Normalize();
        CanViewPatient = p.CanView;
        CanAddPatientData = p.CanAdd;
        CanEditPatientData = p.CanEdit;
        CanDeletePatientData = p.CanDelete;
        CanManageMembers = p.CanManageMembers;
        Status = InvitationStatus.Pending;
        ExpiresAt = expiresAt;
    }

    public Guid PatientId { get; private set; }
    public string InvitedEmail { get; private set; } = string.Empty;
    public Guid InvitedByUserId { get; private set; }
    public CareRole Role { get; private set; }
    public string RelationshipType { get; private set; } = string.Empty;
    public bool CanManageMembers { get; private set; }
    public bool CanViewPatient { get; private set; }
    public bool CanAddPatientData { get; private set; }
    public bool CanEditPatientData { get; private set; }
    public bool CanDeletePatientData { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public Guid? AcceptedByUserId { get; private set; }

    public Patient? Patient { get; private set; }

    public string AuditEntityType => "PATIENT_INVITATION";
    public Guid? AuditPatientId => PatientId;

    public PermissionSet Permissions =>
        new(CanViewPatient, CanAddPatientData, CanEditPatientData, CanDeletePatientData, CanManageMembers);

    /// <summary>Indica se o convite ainda pode ser respondido.</summary>
    public bool IsOpen(DateTimeOffset now) => Status == InvitationStatus.Pending && ExpiresAt > now;

    /// <summary>
    /// Aceita o convite. Regras: precisa estar pendente, não expirado e o e-mail verificado do usuário deve coincidir.
    /// </summary>
    public void Accept(Guid userId, string userEmail, DateTimeOffset now)
    {
        EnsureOpen(now);
        if (!string.Equals(InvitedEmail, userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Este convite foi enviado para outro e-mail.", "INVITATION_EMAIL_MISMATCH");
        Status = InvitationStatus.Accepted;
        AcceptedByUserId = userId;
        RespondedAt = now;
    }

    /// <summary>Recusa o convite.</summary>
    public void Decline(string userEmail, DateTimeOffset now)
    {
        EnsureOpen(now);
        if (!string.Equals(InvitedEmail, userEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Este convite foi enviado para outro e-mail.", "INVITATION_EMAIL_MISMATCH");
        Status = InvitationStatus.Declined;
        RespondedAt = now;
    }

    /// <summary>Cancela o convite (por um gestor da rede de cuidado).</summary>
    public void Revoke(DateTimeOffset now)
    {
        if (Status != InvitationStatus.Pending)
            throw new BusinessRuleException("INVITATION_NOT_PENDING", "Somente convites pendentes podem ser cancelados.");
        Status = InvitationStatus.Revoked;
        RespondedAt = now;
    }

    /// <summary>Marca como expirado quando o prazo é ultrapassado.</summary>
    public void ExpireIfNeeded(DateTimeOffset now)
    {
        if (Status == InvitationStatus.Pending && ExpiresAt <= now) Status = InvitationStatus.Expired;
    }

    private void EnsureOpen(DateTimeOffset now)
    {
        ExpireIfNeeded(now);
        if (Status == InvitationStatus.Expired)
            throw new BusinessRuleException("INVITATION_EXPIRED", "O convite expirou. Solicite um novo convite ao administrador.");
        if (Status != InvitationStatus.Pending)
            throw new BusinessRuleException("INVITATION_NOT_PENDING", "O convite não está mais pendente.");
    }
}
