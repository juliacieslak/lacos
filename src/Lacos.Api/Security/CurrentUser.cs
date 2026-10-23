using System.Security.Claims;
using Lacos.Application.Interfaces;
using Lacos.Infrastructure.Integrations.ExternalApis.Auth0;
using Microsoft.Extensions.Options;

namespace Lacos.Api.Security;

/// <summary>
/// Implementação de <see cref="ICurrentUser"/> baseada no token JWT do Auth0 e no contexto HTTP.
/// O Auth0 não inclui e-mail/nome no access token por padrão: uma Auth0 Action deve adicioná-los como claims
/// com namespace (ex.: https://lacos.app/email). Também são aceitas as claims padrão OIDC ("email", "name").
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    private readonly string _ns;

    public CurrentUser(IHttpContextAccessor accessor, IOptions<Auth0Options> options)
    {
        _accessor = accessor;
        _ns = options.Value.ClaimsNamespace;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public string? Auth0UserId => Claim("sub");
    public string? Email => Claim(_ns + "email") ?? Claim("email");
    public bool EmailVerified => string.Equals(Claim(_ns + "email_verified") ?? Claim("email_verified"), "true", StringComparison.OrdinalIgnoreCase);
    public string? Name => Claim(_ns + "name") ?? Claim("name");
    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private string? Claim(string type) => Principal?.FindFirst(type)?.Value;
}
