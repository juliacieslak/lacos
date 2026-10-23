using Lacos.Application.Common;
using Lacos.Application.Services;
using Lacos.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Lacos.Api.Controllers;

/// <summary>
/// Controller genérico (DRY) para registros clínicos do paciente. Expõe o CRUD padrão:
/// GET (lista paginada com período), GET {id}, POST, PUT {id} e DELETE {id}.
/// As permissões (consultar/adicionar/alterar/excluir) são verificadas no serviço.
/// Cada controller concreto define sua rota (ex.: api/patients/{patientId}/symptoms).
/// </summary>
public abstract class PatientRecordControllerBase<TService, TEntity, TRequest, TResponse> : ApiControllerBase
    where TService : PatientRecordService<TEntity, TRequest, TResponse>
    where TEntity : PatientRecord
{
    protected PatientRecordControllerBase(TService service) => Service = service;

    protected TService Service { get; }

    /// <summary>Lista em ordem cronológica decrescente; período opcional [from, to).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TResponse>>> List(Guid patientId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await Service.ListAsync(patientId, from, to, new PageQuery(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TResponse>> Get(Guid patientId, Guid id, CancellationToken ct) =>
        Ok(await Service.GetAsync(patientId, id, ct));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TResponse>> Create(Guid patientId, TRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Service.CreateAsync(patientId, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TResponse>> Update(Guid patientId, Guid id, TRequest request, CancellationToken ct) =>
        Ok(await Service.UpdateAsync(patientId, id, request, ct));

    /// <summary>Exclusão lógica (o histórico é preservado - RF06).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid patientId, Guid id, CancellationToken ct)
    {
        await Service.DeleteAsync(patientId, id, ct);
        return NoContent();
    }
}
