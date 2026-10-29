using Lacos.Api.Security;
using Lacos.Application.DTOs;
using Lacos.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lacos.Api.Controllers;

/// <summary>
/// Serviços de saúde próximos ("Serviços próximos" no menu - WF01/WF02/WF03).
/// As APIs externas são chamadas somente pelo back-end (o front-end nunca as acessa diretamente),
/// o que permite cache, limitação de uso, troca de provedor e proteção de credenciais futuras.
/// </summary>
[Route("api/locations")]
[AllowIncompleteProfile]
public sealed class LocationsController : ApiControllerBase
{
    private readonly IPharmacySearchService _pharmacies;

    public LocationsController(IPharmacySearchService pharmacies) => _pharmacies = pharmacies;

    /// <summary>
    /// Farmácias próximas ao CEP, ordenadas pela distância aproximada.
    /// Erros: 400 CEP inválido · 404 CEP inexistente · 422 coordenadas não encontradas · 503 provedor indisponível.
    /// </summary>
    [HttpGet("pharmacies")]
    [EnableRateLimiting("location-search")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PharmacySearchResponse>> Pharmacies([FromQuery] string cep, [FromQuery] int? radius, CancellationToken ct) =>
        Ok(await _pharmacies.SearchByCepAsync(cep, radius, ct));
}
