using Lacos.Api.Middlewares;
using Lacos.Api.Security;
using Lacos.Application.DTOs;
using Lacos.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Lacos.Api.Controllers;

/// <summary>
/// Sessão do usuário (UC02 Autenticar Usuário). O login/cadastro de credenciais (e-mail e senha - RF02) é feito
/// no Auth0 (Universal Login); o front-end chama estes endpoints com o access token obtido.
/// </summary>
[Route("api/auth")]
[AllowIncompleteProfile]
public sealed class AuthController : ApiControllerBase
{
    private readonly IUserService _users;

    public AuthController(IUserService users) => _users = users;

    /// <summary>
    /// Inicia a sessão: registra o usuário no primeiro acesso e retorna a situação de acesso
    /// (AUTHORIZED → Dashboard; PENDING_APPROVAL → Tela de acesso pendente; NO_INVITATION → Tela sem convite).
    /// </summary>
    [HttpPost("session")]
    public async Task<ActionResult<SessionResponse>> StartSession([FromServices] IOptions<SessionSecurityOptions> session, CancellationToken ct) =>
        Ok(await _users.StartSessionAsync(session.Value.InactivityTimeoutMinutes, ct));

    /// <summary>Encerra a sessão revogando o token atual no back-end (efeito imediato).</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromServices] IDistributedCache cache)
    {
        await SessionActivityMiddleware.RevokeAsync(HttpContext, cache);
        return NoContent();
    }
}
