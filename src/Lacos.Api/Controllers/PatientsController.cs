using Lacos.Application.DTOs;
using Lacos.Application.Services;
using Lacos.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lacos.Api.Controllers;

/// <summary>Perfis de pacientes (RF03, RF05, RF11, UC03, UC11, WF02).</summary>
[Route("api/patients")]
public sealed class PatientsController : ApiControllerBase
{
    private readonly IPatientService _patients;

    public PatientsController(IPatientService patients) => _patients = patients;

    /// <summary>"Meus pacientes": busca por nome e filtro por status.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PatientSummaryResponse>>> List([FromQuery] string? search,
        [FromQuery] PatientCareStatus? status, CancellationToken ct) =>
        Ok(await _patients.ListMineAsync(search, status, ct));

    /// <summary>Cadastra um paciente; o criador torna-se Administrador do Perfil.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<PatientResponse>> Create(CreatePatientRequest request, CancellationToken ct)
    {
        var patient = await _patients.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { patientId = patient.Id }, patient);
    }

    [HttpGet("{patientId:guid}")]
    public async Task<ActionResult<PatientResponse>> Get(Guid patientId, CancellationToken ct) => Ok(await _patients.GetAsync(patientId, ct));

    [HttpPut("{patientId:guid}")]
    public async Task<ActionResult<PatientResponse>> Update(Guid patientId, UpdatePatientRequest request, CancellationToken ct) =>
        Ok(await _patients.UpdateAsync(patientId, request, ct));

    [HttpPatch("{patientId:guid}/status")]
    public async Task<ActionResult<PatientResponse>> ChangeStatus(Guid patientId, ChangeCareStatusRequest request, CancellationToken ct) =>
        Ok(await _patients.ChangeCareStatusAsync(patientId, request, ct));

    /// <summary>Unidades de medida do paciente (RF09).</summary>
    [HttpPut("{patientId:guid}/measurement-units")]
    public async Task<ActionResult<PatientResponse>> UpdateUnits(Guid patientId, UpdateMeasurementUnitsRequest request, CancellationToken ct) =>
        Ok(await _patients.UpdateMeasurementUnitsAsync(patientId, request, ct));

    /// <summary>Exclusão definitiva do paciente (LGPD). Exige o código público como confirmação.</summary>
    [HttpDelete("{patientId:guid}")]
    [EnableRateLimiting("sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Erase(Guid patientId, ErasePatientRequest request, CancellationToken ct)
    {
        await _patients.EraseAsync(patientId, request, ct);
        return NoContent();
    }
}
