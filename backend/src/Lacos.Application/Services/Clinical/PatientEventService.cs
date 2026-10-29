using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services.Clinical;

/// <summary>
/// Agenda do paciente: consultas, exames, tratamentos e outras atividades (RF18 / WF03).
/// </summary>
public sealed class PatientEventService : PatientRecordService<PatientEvent, PatientEventRequest, PatientEventResponse>
{
    private readonly IPatientEventRepository _events;

    public PatientEventService(IPatientEventRepository repository, PatientRecordDependencies deps) : base(repository, deps)
        => _events = repository;

    protected override string ResourceName => "Compromisso";

    protected override Task<PatientEvent> CreateEntityAsync(PatientAccess access, PatientEventRequest r, CancellationToken ct) =>
        Task.FromResult(new PatientEvent(access.Patient.Id, access.UserId, r.EventType, r.Title, r.ScheduledAt,
            r.DurationMinutes, r.LocationName, r.ProviderName, r.Notes));

    protected override Task ApplyUpdateAsync(PatientAccess access, PatientEvent e, PatientEventRequest r, CancellationToken ct)
    {
        e.Update(r.EventType, r.Title, r.ScheduledAt, r.DurationMinutes, r.LocationName, r.ProviderName, r.Notes, access.UserId);
        return Task.CompletedTask;
    }

    protected override PatientEventResponse Map(PatientEvent e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.EventType, e.Title, e.ScheduledAt, e.DurationMinutes, e.EndsAt, e.LocationName,
            e.ProviderName, e.Status, e.Summary, e.Notes, e.ToRecordInfo(names));

    /// <summary>Lista compromissos de um período com filtros de tipo e status, em ordem cronológica.</summary>
    public async Task<IReadOnlyList<PatientEventResponse>> ListRangeAsync(Guid patientId, DateTimeOffset from, DateTimeOffset to,
        PatientEventType? type, PatientEventStatus? status, CancellationToken ct = default)
    {
        if (to <= from) throw new ValidationFailedException("to", "O fim do período deve ser posterior ao início.");
        if ((to - from).TotalDays > 366) throw new ValidationFailedException("to", "O período máximo é de 1 ano.");
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.View, ct);
        var events = await _events.ListInRangeAsync(new[] { patientId }, from, to, type, status, ct);
        await Deps.Audit.RecordViewAsync(access.UserId, patientId, "PATIENT_EVENT_LIST", null, ct);
        var names = await LoadNamesAsync(events, ct);
        return events.Select(e => Map(e, names)).ToList();
    }

    /// <summary>
    /// Altera o status (concluir, cancelar, marcar como perdido, reagendar). Auditado como STATUS_CHANGE.
    /// Permissão: alterar.
    /// </summary>
    public async Task<PatientEventResponse> ChangeStatusAsync(Guid patientId, Guid id, ChangeEventStatusRequest r, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Edit, ct);
        var ev = await FindAsync(patientId, id, ct);
        ev.ChangeStatus(r.Status, r.Summary, access.UserId);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
        return await MapAsync(ev, ct);
    }

    /// <summary>Registra o resumo da ocasião/resultado do atendimento (RF18).</summary>
    public async Task<PatientEventResponse> SetSummaryAsync(Guid patientId, Guid id, EventSummaryRequest r, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Edit, ct);
        var ev = await FindAsync(patientId, id, ct);
        ev.SetSummary(r.Summary, access.UserId);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
        return await MapAsync(ev, ct);
    }
}
