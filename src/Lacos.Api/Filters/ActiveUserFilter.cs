using Lacos.Api.Security;
using Lacos.Application.Services;
using Lacos.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lacos.Api.Filters;

/// <summary>
/// Filtro global que garante, antes de qualquer operação sobre dados:
/// 1. Usuário registrado e ativo no sistema (após POST /api/auth/session).
/// 2. Cadastro (onboarding - RF01) concluído.
/// 3. Consentimentos LGPD vigentes (RNF01) — sem eles, nenhum dado de saúde é tratado.
/// Endpoints com [AllowAnonymous] ou [AllowIncompleteProfile] são dispensados.
/// </summary>
public sealed class ActiveUserFilter : IAsyncActionFilter
{
    private readonly ICurrentUserContext _userContext;
    private readonly IUserService _users;

    public ActiveUserFilter(ICurrentUserContext userContext, IUserService users)
    {
        _userContext = userContext;
        _users = users;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || metadata.OfType<AllowIncompleteProfileAttribute>().Any())
        {
            await next();
            return;
        }

        var user = await _userContext.GetRequiredUserAsync(context.HttpContext.RequestAborted);
        if (!user.ProfileCompleted)
            throw new ForbiddenException("Conclua o cadastro inicial para continuar.", "ONBOARDING_REQUIRED");
        if (!await _users.HasCurrentConsentsAsync(user.Id, context.HttpContext.RequestAborted))
            throw new ForbiddenException("É necessário aceitar a versão vigente dos termos de uso e do consentimento LGPD.", "CONSENT_REQUIRED");

        await next();
    }
}
