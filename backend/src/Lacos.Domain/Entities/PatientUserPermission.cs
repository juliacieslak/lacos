using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Vínculo usuário x paciente com as permissões de acesso (tabela <c>patient_user_permissions</c>).
///
/// Regras de negócio:
/// - O profile_type do usuário não concede acesso automático: somente um vínculo ativo concede acesso (CSV).
/// - O administrador define quais ações cada usuário pode realizar: consultar, adicionar, alterar ou excluir (RF07).
///   O modelo de dados não possuía a flag de exclusão; foi acrescentada <see cref="CanDeletePatientData"/> para cumprir RF07.
/// - can_manage_members permite adicionar, remover ou gerenciar membros da rede de cuidado (UC04/UC05).
/// - A revogação tem efeito imediato (US01): as permissões são lidas do banco a cada requisição, sem cache.
/// - Quem pode adicionar/editar/excluir precisa conseguir visualizar; por isso essas flags implicam can_view_patient.
/// </summary>
public class PatientUserPermission : BaseEntity, IAuditable
{
    private PatientUserPermission() { }

    public PatientUserPermission(Guid patientId, Guid userId, CareRole role, string relationshipType, PermissionSet permissions)
    {
        PatientId = patientId;
        UserId = userId;
        Role = role;
        RelationshipType = relationshipType.Trim();
        IsActive = true;
        Apply(permissions);
    }

    public Guid PatientId { get; private set; }
    public Guid UserId { get; private set; }
    public CareRole Role { get; private set; }
    public string RelationshipType { get; private set; } = string.Empty;
    public bool CanManageMembers { get; private set; }
    public bool CanViewPatient { get; private set; }
    public bool CanAddPatientData { get; private set; }
    public bool CanEditPatientData { get; private set; }
    public bool CanDeletePatientData { get; private set; }
    public bool IsActive { get; private set; }

    public User? User { get; private set; }
    public Patient? Patient { get; private set; }

    public string AuditEntityType => "PATIENT_USER_PERMISSION";
    public Guid? AuditPatientId => PatientId;

    /// <summary>Cria o vínculo do criador do paciente, que se torna Administrador do Perfil com todas as permissões (UC03).</summary>
    public static PatientUserPermission CreateAdministrator(Guid patientId, Guid userId, string relationshipType) =>
        new(patientId, userId, CareRole.Administrator, relationshipType, PermissionSet.FromPreset(AccessProfilePreset.Manager));

    /// <summary>Altera papel, relação e permissões do membro (UC04).</summary>
    public void Update(CareRole role, string relationshipType, PermissionSet permissions)
    {
        if (!IsActive) throw new BusinessRuleException("PERMISSION_REVOKED", "Não é possível alterar um vínculo revogado.");
        Role = role;
        RelationshipType = relationshipType.Trim();
        Apply(permissions);
    }

    /// <summary>Revoga o acesso imediatamente (UC05). O registro é mantido para histórico.</summary>
    public void Revoke()
    {
        IsActive = false;
        CanManageMembers = false;
        CanViewPatient = false;
        CanAddPatientData = false;
        CanEditPatientData = false;
        CanDeletePatientData = false;
    }

    /// <summary>Reativa um vínculo revogado anteriormente (ex.: novo convite aceito).</summary>
    public void Reactivate(CareRole role, string relationshipType, PermissionSet permissions)
    {
        IsActive = true;
        Role = role;
        RelationshipType = relationshipType.Trim();
        Apply(permissions);
    }

    /// <summary>Retorna as permissões efetivas (todas falsas quando o vínculo está inativo).</summary>
    public PermissionSet ToPermissionSet() => IsActive
        ? new PermissionSet(CanViewPatient, CanAddPatientData, CanEditPatientData, CanDeletePatientData, CanManageMembers)
        : PermissionSet.None;

    private void Apply(PermissionSet p)
    {
        var normalized = p.Normalize();
        CanViewPatient = normalized.CanView;
        CanAddPatientData = normalized.CanAdd;
        CanEditPatientData = normalized.CanEdit;
        CanDeletePatientData = normalized.CanDelete;
        CanManageMembers = normalized.CanManageMembers;
    }
}

/// <summary>
/// Conjunto imutável de permissões granulares (RF07).
/// </summary>
public sealed record PermissionSet(bool CanView, bool CanAdd, bool CanEdit, bool CanDelete, bool CanManageMembers)
{
    public static readonly PermissionSet None = new(false, false, false, false, false);

    /// <summary>
    /// Converte os perfis pré-definidos de US01 ("Apenas Leitura" e "Edição") em permissões granulares.
    /// </summary>
    public static PermissionSet FromPreset(AccessProfilePreset preset) => preset switch
    {
        AccessProfilePreset.ReadOnly => new(true, false, false, false, false),
        AccessProfilePreset.Editor => new(true, true, true, true, false),
        AccessProfilePreset.Manager => new(true, true, true, true, true),
        _ => throw new ArgumentOutOfRangeException(nameof(preset), "Preset personalizado exige informar as permissões.")
    };

    /// <summary>
    /// Normaliza dependências entre permissões: qualquer permissão de escrita ou de gestão implica visualizar.
    /// Isso evita vínculos incoerentes (ex.: alguém que adiciona dados mas não consegue vê-los).
    /// </summary>
    public PermissionSet Normalize()
    {
        var mustView = CanView || CanAdd || CanEdit || CanDelete || CanManageMembers;
        return this with { CanView = mustView };
    }

    public bool HasAny => CanView || CanAdd || CanEdit || CanDelete || CanManageMembers;
}
