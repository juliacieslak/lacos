using Lacos.Api.Security;
using Lacos.Application.DTOs;
using Lacos.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lacos.Api.Controllers;

/// <summary>Rede de cuidado do paciente: membros, permissões e convites (UC04, UC05, RF07, US01).</summary>
[Route("api/patients/{patientId:guid}")]
public sealed class CareTeamController : ApiControllerBase
{
    private readonly ICareTeamService _careTeam;

    public CareTeamController(ICareTeamService careTeam) => _careTeam = careTeam;

    [HttpGet("members")]
    public async Task<ActionResult<IReadOnlyList<CareTeamMemberResponse>>> ListMembers(Guid patientId, [FromQuery] bool includeInactive,
        CancellationToken ct) => Ok(await _careTeam.ListMembersAsync(patientId, includeInactive, ct));

    [HttpPut("members/{permissionId:guid}")]
    public async Task<ActionResult<CareTeamMemberResponse>> UpdateMember(Guid patientId, Guid permissionId, UpdateMemberRequest request,
        CancellationToken ct) => Ok(await _careTeam.UpdateMemberAsync(patientId, permissionId, request, ct));

    /// <summary>Revoga o acesso (efeito imediato). Também usado pelo próprio membro para sair da rede.</summary>
    [HttpDelete("members/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeMember(Guid patientId, Guid permissionId, [FromBody] RevokeMemberRequest? request, CancellationToken ct)
    {
        await _careTeam.RevokeMemberAsync(patientId, permissionId, request ?? new RevokeMemberRequest(null), ct);
        return NoContent();
    }

    [HttpGet("invitations")]
    public async Task<ActionResult<IReadOnlyList<InvitationResponse>>> ListInvitations(Guid patientId, CancellationToken ct) =>
        Ok(await _careTeam.ListInvitationsAsync(patientId, ct));

    [HttpPost("invitations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<InvitationResponse>> Invite(Guid patientId, InviteMemberRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await _careTeam.InviteAsync(patientId, request, ct));

    [HttpDelete("invitations/{invitationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeInvitation(Guid patientId, Guid invitationId, CancellationToken ct)
    {
        await _careTeam.RevokeInvitationAsync(patientId, invitationId, ct);
        return NoContent();
    }
}

/// <summary>Convites recebidos pelo usuário autenticado ("Tela de acesso pendente").</summary>
[Route("api/invitations")]
public sealed class InvitationsController : ApiControllerBase
{
    private readonly ICareTeamService _careTeam;

    public InvitationsController(ICareTeamService careTeam) => _careTeam = careTeam;

    [HttpGet("mine")]
    [AllowIncompleteProfile]
    public async Task<ActionResult<IReadOnlyList<InvitationResponse>>> Mine(CancellationToken ct) => Ok(await _careTeam.ListMyInvitationsAsync(ct));

    [HttpPost("{invitationId:guid}/accept")]
    public async Task<ActionResult<CareTeamMemberResponse>> Accept(Guid invitationId, CancellationToken ct) =>
        Ok(await _careTeam.AcceptInvitationAsync(invitationId, ct));

    [HttpPost("{invitationId:guid}/decline")]
    [AllowIncompleteProfile]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Decline(Guid invitationId, CancellationToken ct)
    {
        await _careTeam.DeclineInvitationAsync(invitationId, ct);
        return NoContent();
    }
}
