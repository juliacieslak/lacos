using Lacos.Domain.Enums;

namespace Lacos.Application.DTOs;

/// <summary>Membro da rede de cuidado (US01 / WF01 "Quem está cuidando").</summary>
public sealed record CareTeamMemberResponse(
    Guid PermissionId, Guid UserId, string UserPublicCode, string Name, string Email, ProfileType? ProfileType,
    CareRole Role, string RelationshipType, PermissionsDto Permissions, bool IsActive, DateTimeOffset Since, DateTimeOffset UpdatedAt);

/// <summary>
/// Convite (US01): e-mail do convidado, papel, relação e perfil de acesso.
/// Quando <see cref="Preset"/> = Custom, <see cref="Permissions"/> é obrigatório.
/// </summary>
public sealed record InviteMemberRequest(
    string Email, CareRole Role, string RelationshipType, AccessProfilePreset Preset, PermissionsDto? Permissions);

/// <summary>Alteração de papel/permissões de um membro (UC04).</summary>
public sealed record UpdateMemberRequest(
    CareRole Role, string RelationshipType, AccessProfilePreset Preset, PermissionsDto? Permissions);

public sealed record RevokeMemberRequest(string? Reason);

public sealed record InvitationResponse(
    Guid Id, Guid PatientId, string PatientName, string InvitedEmail, CareRole Role, string RelationshipType,
    PermissionsDto Permissions, InvitationStatus Status, DateTimeOffset ExpiresAt, string? InvitedByName, DateTimeOffset CreatedAt);
