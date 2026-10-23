using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services.Clinical;

/// <summary>
/// Rotina de cuidado: tarefas planejadas e marcação de concluídas (WF01/WF03, validação com cuidador da Sprint 3).
/// </summary>
public sealed class CareRoutineService : PatientRecordService<CareRoutineTask, CareRoutineTaskRequest, CareRoutineTaskResponse>
{
    private readonly ICareRoutineRepository _routines;

    public CareRoutineService(ICareRoutineRepository repository, PatientRecordDependencies deps) : base(repository, deps)
        => _routines = repository;

    protected override string ResourceName => "Tarefa da rotina de cuidado";

    protected override Task<CareRoutineTask> CreateEntityAsync(PatientAccess access, CareRoutineTaskRequest r, CancellationToken ct) =>
        Task.FromResult(new CareRoutineTask(access.Patient.Id, access.UserId, r.Title, r.Description, r.Category, r.ScheduledTime,
            r.DaysOfWeek ?? Array.Empty<DayOfWeek>(), r.StartDate ?? Deps.Clock.LocalToday, r.EndDate));

    protected override Task ApplyUpdateAsync(PatientAccess access, CareRoutineTask e, CareRoutineTaskRequest r, CancellationToken ct)
    {
        e.Update(r.Title, r.Description, r.Category, r.ScheduledTime, r.DaysOfWeek ?? Array.Empty<DayOfWeek>(),
            r.StartDate ?? e.StartDate, r.EndDate, access.UserId);
        return Task.CompletedTask;
    }

    protected override CareRoutineTaskResponse Map(CareRoutineTask e, IReadOnlyDictionary<Guid, string> names) =>
        new(e.Id, e.PatientId, e.Title, e.Description, e.Category, e.ScheduledTime, e.DaysOfWeek, e.StartDate, e.EndDate, e.ToRecordInfo(names));

    /// <summary>
    /// Marca a tarefa como realizada em uma data ("ação rápida" pedida pelo cuidador).
    /// Regras: a tarefa deve ocorrer naquela data; não é possível concluir datas futuras; idempotente por (tarefa, data).
    /// Permissão: adicionar.
    /// </summary>
    public async Task<CareRoutineCompletionResponse> CompleteAsync(Guid patientId, Guid taskId, CompleteCareTaskRequest r, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Add, ct);
        var task = await FindAsync(patientId, taskId, ct);
        var date = r.Date ?? Deps.Clock.LocalToday;
        if (date > Deps.Clock.LocalToday)
            throw new BusinessRuleException("FUTURE_COMPLETION", "Não é possível concluir uma tarefa em data futura.");
        if (!task.OccursOn(date))
            throw new BusinessRuleException("TASK_NOT_SCHEDULED", "A tarefa não está programada para esta data.");

        var existing = await _routines.GetCompletionAsync(taskId, date, ct);
        if (existing is not null) return ToResponse(existing);

        var completion = new CareRoutineCompletion(patientId, taskId, access.UserId, date, Deps.Clock.UtcNow, r.Notes);
        await _routines.AddCompletionAsync(completion, ct);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
        return ToResponse(completion);
    }

    /// <summary>Desfaz a conclusão (exclusão lógica, mantendo o histórico). Permissão: alterar.</summary>
    public async Task UncompleteAsync(Guid patientId, Guid taskId, DateOnly date, CancellationToken ct = default)
    {
        var access = await Deps.Access.RequireAsync(patientId, PatientPermission.Edit, ct);
        await FindAsync(patientId, taskId, ct);
        var completion = await _routines.GetCompletionAsync(taskId, date, ct)
                         ?? throw new NotFoundException("Conclusão da tarefa", $"{taskId}/{date:yyyy-MM-dd}");
        completion.MarkAsDeleted(access.UserId, Deps.Clock.UtcNow);
        await Deps.UnitOfWork.SaveChangesAsync(ct);
    }

    private static CareRoutineCompletionResponse ToResponse(CareRoutineCompletion c) =>
        new(c.Id, c.TaskId, c.ScheduledDate, c.CompletedAt, c.Notes, c.CreatedByUserId);
}
