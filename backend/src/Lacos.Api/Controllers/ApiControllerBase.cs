using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lacos.Api.Controllers;

/// <summary>
/// Base de todos os controllers: exige autenticação (token Auth0) e define o formato JSON.
/// Os controllers são finos: apenas traduzem HTTP ↔ casos de uso; as regras ficam na camada de aplicação/domínio.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public abstract class ApiControllerBase : ControllerBase
{
}
