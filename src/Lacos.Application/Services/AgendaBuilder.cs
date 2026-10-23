using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Monta a agenda consolidada de um conjunto de pacientes em um período (WF01 "Agenda de hoje" e WF03 "Agenda de cuidados").
/// Não faz verificação de permissão: é um componente interno chamado por serviços que já autorizaram o acesso.
/// </summary>
public interface IAgendaBuilder
{
    Task<IReadOnlyList<AgendaItemResponse>> BuildAsync(IReadOnlyDictionary<Guid, string> patients, DateOnly from, DateOnly to,
        CancellationToken ct = default);
}

public sealed class AgendaBuilder : IAgendaBuilder
{
    /// <summary>Limite de dias por consulta para manter o tempo de resposta (RNF09) e evitar abuso.</summary>
    public const int MaxDays = 42;

    private readonly IPatientEventRepository _events;
    private readonly IMedicationRepository _medications;
    private readonly IMedicationAdministrationRepository _administrations;
    private readonly ICareRoutineRepository _routines;
    private readonly IDateTimeProvider _clock;

    public AgendaBuilder(IPatientEventRepository events, IMedicationRepository medications,
        IMedicationAdministrationRepository administrations, ICareRoutineRepository routines, IDateTimeProvider clock)
    {
        _events = events;
        _medications = medications;
        _administrations = administrations;
        _routines = routines;
        _clock = clock;
    }

    /// <summary>
    /// Algoritmo:
    /// 1. Converte o período local [from, to] em instantes UTC [início de from, início de to+1).
    /// 2. Compromissos: consulta direta por período.
    /// 3. Medicamentos: expande, dia a dia, os horários programados de cada medicamento ativo naquela data e
    ///    cruza com as administrações registradas (chave medicamento + horário programado) para marcar "feito".
    /// 4. Rotina: expande as tarefas que ocorrem em cada data e cruza com as conclusões (chave tarefa + data).
    /// 5. Ordena tudo por horário. Itens não concluídos com horário já passado são marcados como atrasados.
    /// </summary>
    public async Task<IReadOnlyList<AgendaItemResponse>> BuildAsync(IReadOnlyDictionary<Guid, string> patients, DateOnly from,
        DateOnly to, CancellationToken ct = default)
    {
        if (patients.Count == 0) return Array.Empty<AgendaItemResponse>();
        if (to < from) (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber + 1 > MaxDays) to = from.AddDays(MaxDays - 1);

        var ids = patients.Keys.ToList();
        var start = _clock.ToInstant(from, TimeOnly.MinValue);
        var end = _clock.ToInstant(to.AddDays(1), TimeOnly.MinValue);
        var now = _clock.UtcNow;
        var items = new List<AgendaItemResponse>();

        // 2. Compromissos da agenda (RF18).
        foreach (var ev in await _events.ListInRangeAsync(ids, start, end, null, null, ct))
        {
            var done = ev.Status == PatientEventStatus.Completed;
            items.Add(new AgendaItemResponse(AgendaItemKind.Event, ev.Id, ev.PatientId, patients[ev.PatientId], ev.Title,
                ev.ProviderName is null ? ev.LocationName : $"{ev.ProviderName}{(ev.LocationName is null ? "" : " — " + ev.LocationName)}",
                ev.ScheduledAt, ev.EndsAt, done, !done && ev.Status == PatientEventStatus.Scheduled && ev.ScheduledAt < now,
                ev.Status, ev.EventType, ev.LocationName, ev.ProviderName, null, null, null, null));
        }

        // 3. Doses programadas de medicamentos (RF08) e situação de administração (US02).
        var meds = await _medications.ListByPatientsAsync(ids, activeOnly: false, from, ct);
        var administrations = (await _administrations.ListByMedicationsAsync(meds.Select(m => m.Id), start, end, ct))
            .Where(a => a.ScheduledFor.HasValue)
            .GroupBy(a => (a.MedicationId, a.ScheduledFor!.Value))
            .ToDictionary(g => g.Key, g => g.First());
        foreach (var med in meds)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                foreach (var time in med.GetScheduledTimes(date))
                {
                    var at = _clock.ToInstant(date, time);
                    administrations.TryGetValue((med.Id, at), out var adm);
                    items.Add(new AgendaItemResponse(AgendaItemKind.Medication, med.Id, med.PatientId, patients[med.PatientId],
                        med.Name, $"{med.Dosage}{(string.IsNullOrEmpty(med.Brand) ? "" : " — " + med.Brand)}", at, null,
                        adm is not null, adm is null && at < now, null, null, null, null, adm?.Id, adm?.Status, date, null));
                }
            }
        }

        // 4. Tarefas da rotina de cuidado e suas conclusões.
        var tasks = await _routines.ListByPatientsAsync(ids, ct);
        var completions = (await _routines.ListCompletionsAsync(tasks.Select(t => t.Id), from, to, ct))
            .ToDictionary(c => (c.TaskId, c.ScheduledDate));
        foreach (var task in tasks)
        {
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                if (!task.OccursOn(date)) continue;
                var at = _clock.ToInstant(date, task.ScheduledTime);
                completions.TryGetValue((task.Id, date), out var completion);
                items.Add(new AgendaItemResponse(AgendaItemKind.CareTask, task.Id, task.PatientId, patients[task.PatientId],
                    task.Title, task.Description, at, null, completion is not null, completion is null && at < now,
                    null, null, null, null, null, null, date, completion?.Id));
            }
        }

        return items.OrderBy(i => i.StartsAt).ThenBy(i => i.PatientName).ToList();
    }
}
