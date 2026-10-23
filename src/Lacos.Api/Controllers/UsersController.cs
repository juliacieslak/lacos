using Lacos.Api.Security;
using Lacos.Application.DTOs;
using Lacos.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lacos.Api.Controllers;

/// <summary>Perfil do usuário autenticado, onboarding (RF01 / UC01), consentimentos e exclusão da conta (LGPD).</summary>
[Route("api/users/me")]
[AllowIncompleteProfile]
public sealed class UsersController : ApiControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get(CancellationToken ct) => Ok(await _users.GetMeAsync(ct));

    /// <summary>Conclui o cadastro inicial (RF01) com aceite dos termos e do tratamento de dados sensíveis.</summary>
    [HttpPost("onboarding")]
    public async Task<ActionResult<UserResponse>> CompleteOnboarding(CompleteOnboardingRequest request, CancellationToken ct) =>
        Ok(await _users.CompleteOnboardingAsync(request, ct));

    [HttpPut]
    public async Task<ActionResult<UserResponse>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _users.UpdateProfileAsync(request, ct));

    [HttpGet("consents")]
    public async Task<ActionResult<ConsentStatusResponse>> GetConsents(CancellationToken ct) => Ok(await _users.GetConsentsAsync(ct));

    [HttpPost("consents")]
    public async Task<ActionResult<ConsentStatusResponse>> AcceptConsents(AcceptConsentsRequest request, CancellationToken ct) =>
        Ok(await _users.AcceptConsentsAsync(request, ct));

    [HttpPost("consents/revoke")]
    public async Task<ActionResult<ConsentStatusResponse>> RevokeConsent(RevokeConsentRequest request, CancellationToken ct) =>
        Ok(await _users.RevokeConsentAsync(request.ConsentType, ct));

    /// <summary>Exclusão definitiva da conta e anonimização dos dados pessoais (RNF01).</summary>
    [HttpDelete]
    [EnableRateLimiting("sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(DeleteAccountRequest request, CancellationToken ct)
    {
        await _users.DeleteAccountAsync(request, ct);
        return NoContent();
    }
}
