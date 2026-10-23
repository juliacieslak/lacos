using Lacos.Application.DTOs;
using Lacos.Application.Services.Clinical;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Lacos.Api.Controllers;

/// <summary>Sintomas, queixas e alterações percebidas (RF09). Rota: /api/patients/{patientId}/symptoms.</summary>
[Route("api/patients/{patientId:guid}/symptoms")]
public sealed class SymptomsController : PatientRecordControllerBase<SymptomService, SymptomRecord, SymptomRequest, SymptomResponse>
{
    public SymptomsController(SymptomService service) : base(service) { }
}

/// <summary>Diário de acompanhamento (RF04). Rota: /api/patients/{patientId}/care-diary.</summary>
[Route("api/patients/{patientId:guid}/care-diary")]
public sealed class CareDiaryController : PatientRecordControllerBase<CareDiaryService, CareDiaryEntry, CareDiaryRequest, CareDiaryResponse>
{
    public CareDiaryController(CareDiaryService service) : base(service) { }
}

/// <summary>Condições de saúde com CID (RF14).</summary>
[Route("api/patients/{patientId:guid}/health-conditions")]
public sealed class HealthConditionsController
    : PatientRecordControllerBase<HealthConditionService, HealthCondition, HealthConditionRequest, HealthConditionResponse>
{
    public HealthConditionsController(HealthConditionService service) : base(service) { }
}

/// <summary>Histórico de cirurgias (RF15).</summary>
[Route("api/patients/{patientId:guid}/surgeries")]
public sealed class SurgeriesController : PatientRecordControllerBase<SurgeryService, Surgery, SurgeryRequest, SurgeryResponse>
{
    public SurgeriesController(SurgeryService service) : base(service) { }
}

/// <summary>Alergias (RF16).</summary>
[Route("api/patients/{patientId:guid}/allergies")]
public sealed class AllergiesController : PatientRecordControllerBase<AllergyService, Allergy, AllergyRequest, AllergyResponse>
{
    public AllergiesController(AllergyService service) : base(service) { }
}

/// <summary>Métricas de saúde (RF09) com alertas de limite (RF13).</summary>
[Route("api/patients/{patientId:guid}/measurements")]
public sealed class MeasurementsController
    : PatientRecordControllerBase<HealthMeasurementService, HealthMeasurement, HealthMeasurementRequest, HealthMeasurementResponse>
{
    public MeasurementsController(HealthMeasurementService service) : base(service) { }
}

/// <summary>Medicamentos (RF08).</summary>
[Route("api/patients/{patientId:guid}/medications")]
public sealed class MedicationsController : PatientRecordControllerBase<MedicationService, Medication, MedicationRequest, MedicationResponse>
{
    public MedicationsController(MedicationService service) : base(service) { }

    /// <summary>Medicamentos (todos ou apenas os em uso hoje), sem paginação, para a tela de medicação.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<IReadOnlyList<MedicationResponse>>> Current(Guid patientId, [FromQuery] bool activeOnly = true,
        CancellationToken ct = default) => Ok(await Service.ListCurrentAsync(patientId, activeOnly, ct));

    /// <summary>Encerra o uso do medicamento (define a data de fim), mantendo o histórico.</summary>
    [HttpPost("{id:guid}/discontinue")]
    public async Task<ActionResult<MedicationResponse>> Discontinue(Guid patientId, Guid id, [FromQuery] DateOnly? endDate, CancellationToken ct) =>
        Ok(await Service.DiscontinueAsync(patientId, id, endDate, ct));
}

/// <summary>Registro de doses administradas/não administradas (US02).</summary>
[Route("api/patients/{patientId:guid}/medication-administrations")]
public sealed class MedicationAdministrationsController
    : PatientRecordControllerBase<MedicationAdministrationService, MedicationAdministration, MedicationAdministrationRequest, MedicationAdministrationResponse>
{
    public MedicationAdministrationsController(MedicationAdministrationService service) : base(service) { }
}

/// <summary>Agenda do paciente: consultas, exames, tratamentos (RF18).</summary>
[Route("api/patients/{patientId:guid}/events")]
public sealed class EventsController : PatientRecordControllerBase<PatientEventService, PatientEvent, PatientEventRequest, PatientEventResponse>
{
    public EventsController(PatientEventService service) : base(service) { }

    /// <summary>Compromissos de um período (ordem cronológica), com filtros de tipo e status.</summary>
    [HttpGet("range")]
    public async Task<ActionResult<IReadOnlyList<PatientEventResponse>>> Range(Guid patientId, [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, [FromQuery] PatientEventType? type, [FromQuery] PatientEventStatus? status, CancellationToken ct) =>
        Ok(await Service.ListRangeAsync(patientId, from, to, type, status, ct));

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<PatientEventResponse>> ChangeStatus(Guid patientId, Guid id, ChangeEventStatusRequest request, CancellationToken ct) =>
        Ok(await Service.ChangeStatusAsync(patientId, id, request, ct));

    /// <summary>Resumo da ocasião / resultado do atendimento.</summary>
    [HttpPut("{id:guid}/summary")]
    public async Task<ActionResult<PatientEventResponse>> Summary(Guid patientId, Guid id, EventSummaryRequest request, CancellationToken ct) =>
        Ok(await Service.SetSummaryAsync(patientId, id, request, ct));
}

/// <summary>Rotina de cuidado: tarefas planejadas e marcação de concluídas (WF01/WF03).</summary>
[Route("api/patients/{patientId:guid}/care-routines")]
public sealed class CareRoutinesController
    : PatientRecordControllerBase<CareRoutineService, CareRoutineTask, CareRoutineTaskRequest, CareRoutineTaskResponse>
{
    public CareRoutinesController(CareRoutineService service) : base(service) { }

    /// <summary>Marca a tarefa como realizada (padrão: hoje).</summary>
    [HttpPost("{id:guid}/completions")]
    public async Task<ActionResult<CareRoutineCompletionResponse>> Complete(Guid patientId, Guid id, CompleteCareTaskRequest request, CancellationToken ct) =>
        Ok(await Service.CompleteAsync(patientId, id, request, ct));

    /// <summary>Desfaz a marcação de realizada de uma data.</summary>
    [HttpDelete("{id:guid}/completions/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Uncomplete(Guid patientId, Guid id, DateOnly date, CancellationToken ct)
    {
        await Service.UncompleteAsync(patientId, id, date, ct);
        return NoContent();
    }
}

/// <summary>Limites clínicos e alertas (RF13).</summary>
[Route("api/patients/{patientId:guid}/health-thresholds")]
public sealed class HealthThresholdsController : ApiControllerBase
{
    private readonly HealthThresholdService _service;

    public HealthThresholdsController(HealthThresholdService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<HealthThresholdResponse>> Get(Guid patientId, CancellationToken ct) => Ok(await _service.GetAsync(patientId, ct));

    /// <summary>Cria ou atualiza os limites. Métricas omitidas permanecem inalteradas.</summary>
    [HttpPut]
    public async Task<ActionResult<HealthThresholdResponse>> Upsert(Guid patientId, HealthThresholdRequest request, CancellationToken ct) =>
        Ok(await _service.UpsertAsync(patientId, request, ct));
}
