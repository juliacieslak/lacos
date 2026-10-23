using Lacos.Application.Interfaces;
using Lacos.Application.Options;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Application.Services;

/// <summary>
/// Geração automática de lembretes e alertas (RF12 / RNF11), executada periodicamente por um worker em segundo plano.
/// Categorias geradas: lembrete de medicamento, medicamento não administrado, lembrete de compromisso
/// e lembrete de rotina de cuidado/aferição.
/// </summary>
public interface ICareReminderService
{
    Task<int> RunAsync(CancellationToken ct = default);
}

public sealed class CareReminderService : ICareReminderService
{
    private readonly IMedicationRepository _medications;
    private readonly IMedicationAdministrationRepository _administrations;
    private readonly IPatientEventRepository _events;
    private readonly ICareRoutineRepository _routines;
    private readonly IPatientRepository _patients;
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ReminderOptions _options;
    private readonly ILogger<CareReminderService> _logger;

    public CareReminderService(IMedicationRepository medications, IMedicationAdministrationRepository administrations,
        IPatientEventRepository events, ICareRoutineRepository routines, IPatientRepository patients, INotificationService notifications,
        IUnitOfWork unitOfWork, IDateTimeProvider clock, IOptions<ReminderOptions> options, ILogger<CareReminderService> logger)
    {
        _medications = medications;
        _administrations = administrations;
        _events = events;
        _routines = routines;
        _patients = patients;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executa uma rodada de verificação. É idempotente: as chaves de deduplicação impedem lembretes repetidos,
    /// portanto pode ser executada a cada minuto e em várias instâncias.
    /// </summary>
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var created = 0;
        created += await MedicationRemindersAsync(now, ct);
        created += await AppointmentRemindersAsync(now, ct);
        created += await CareRoutineCheckinsAsync(now, ct);
        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Rotina de lembretes gerou {Count} notificações", created);
        }
        return created;
    }

    /// <summary>
    /// Medicamentos (hoje e ontem, para cobrir doses próximas da meia-noite):
    /// - Lembrete: dose programada em (agora, agora + antecedência].
    /// - Não administrada: dose cujo horário + tolerância já passou, sem registro de administração,
    ///   programada após o cadastro do medicamento e nas últimas 24 horas.
    /// </summary>
    private async Task<int> MedicationRemindersAsync(DateTimeOffset now, CancellationToken ct)
    {
        var count = 0;
        var today = _clock.ToLocalDate(now);
        var lead = TimeSpan.FromMinutes(_options.MedicationLeadMinutes);
        var tolerance = TimeSpan.FromMinutes(_options.MedicationMissedToleranceMinutes);
        var names = new Dictionary<Guid, string>();

        foreach (var date in new[] { today.AddDays(-1), today })
        {
            var meds = await _medications.ListActiveOnAsync(date, ct);
            if (meds.Count == 0) continue;
            var dayStart = _clock.ToInstant(date, TimeOnly.MinValue);
            var recorded = (await _administrations.ListByMedicationsAsync(meds.Select(m => m.Id), dayStart, dayStart.AddDays(1), ct))
                .Where(a => a.ScheduledFor.HasValue).Select(a => (a.MedicationId, a.ScheduledFor!.Value)).ToHashSet();

            foreach (var med in meds)
            {
                var patientName = await PatientNameAsync(med.PatientId, names, ct);
                if (patientName is null) continue;
                foreach (var time in med.GetScheduledTimes(date))
                {
                    var at = _clock.ToInstant(date, time);
                    if (at > now && at <= now + lead)
                    {
                        count += await _notifications.EnqueueForPatientAsync(med.PatientId, NotificationCategory.MedicationReminder,
                            $"Lembrete: {med.Name}", $"{med.Name} ({med.Dosage}) de {patientName} às {time:HH\\:mm}.",
                            $"med-reminder:{med.Id:N}:{at.UtcTicks}", med.Id, null, ct);
                    }
                    else if (at + tolerance <= now && at > now.AddDays(-1) && at >= med.CreatedAt && !recorded.Contains((med.Id, at)))
                    {
                        count += await _notifications.EnqueueForPatientAsync(med.PatientId, NotificationCategory.MedicationMissed,
                            $"Medicação não registrada: {med.Name}",
                            $"A dose de {med.Name} de {patientName} prevista para {time:HH\\:mm} não foi registrada.",
                            $"med-missed:{med.Id:N}:{at.UtcTicks}", med.Id, null, ct);
                    }
                }
            }
        }
        return count;
    }

    /// <summary>Compromissos agendados: um lembrete para cada antecedência configurada (padrão: 24 h e 1 h antes).</summary>
    private async Task<int> AppointmentRemindersAsync(DateTimeOffset now, CancellationToken ct)
    {
        var count = 0;
        var maxLead = _options.AppointmentReminderLeadMinutes.DefaultIfEmpty(0).Max();
        if (maxLead <= 0) return 0;
        var events = await _events.ListScheduledBetweenAsync(now, now.AddMinutes(maxLead), ct);
        var names = new Dictionary<Guid, string>();
        foreach (var ev in events)
        {
            var patientName = await PatientNameAsync(ev.PatientId, names, ct);
            if (patientName is null) continue;
            // Usa a menor antecedência que já foi alcançada: evita enviar "24 h antes" quando faltam 30 minutos.
            var lead = _options.AppointmentReminderLeadMinutes.OrderBy(l => l).FirstOrDefault(l => ev.ScheduledAt <= now.AddMinutes(l));
            if (lead == 0) continue;
            var local = TimeZoneInfo.ConvertTime(ev.ScheduledAt, _clock.TimeZone);
            count += await _notifications.EnqueueForPatientAsync(ev.PatientId, NotificationCategory.AppointmentReminder,
                $"Lembrete: {ev.Title}", $"{ev.Title} de {patientName} em {local:dd/MM 'às' HH:mm}{(ev.LocationName is null ? "" : " — " + ev.LocationName)}.",
                $"event-reminder:{ev.Id:N}:{lead}:{ev.ScheduledAt.UtcTicks}", ev.Id, null, ct);
        }
        return count;
    }

    /// <summary>Tarefas da rotina de hoje que começam em (agora, agora + antecedência] e ainda não foram concluídas.</summary>
    private async Task<int> CareRoutineCheckinsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var count = 0;
        var today = _clock.ToLocalDate(now);
        var lead = TimeSpan.FromMinutes(_options.CareRoutineLeadMinutes);
        var tasks = (await _routines.ListAllActiveAsync(today, ct)).Where(t => t.OccursOn(today)).ToList();
        if (tasks.Count == 0) return 0;
        var done = (await _routines.ListCompletionsAsync(tasks.Select(t => t.Id), today, today, ct)).Select(c => c.TaskId).ToHashSet();
        var names = new Dictionary<Guid, string>();
        foreach (var task in tasks.Where(t => !done.Contains(t.Id)))
        {
            var at = _clock.ToInstant(today, task.ScheduledTime);
            if (at <= now || at > now + lead) continue;
            var patientName = await PatientNameAsync(task.PatientId, names, ct);
            if (patientName is null) continue;
            count += await _notifications.EnqueueForPatientAsync(task.PatientId, NotificationCategory.CareRoutineCheckin,
                $"Rotina de cuidado: {task.Title}", $"{task.Title} de {patientName} às {task.ScheduledTime:HH\\:mm}.",
                $"routine-checkin:{task.Id:N}:{today:yyyyMMdd}", task.Id, null, ct);
        }
        return count;
    }

    // Busca (com cache local da rodada) o nome do paciente; retorna null para pacientes inativos/excluídos.
    private async Task<string?> PatientNameAsync(Guid patientId, Dictionary<Guid, string> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(patientId, out var name)) return name;
        var patient = await _patients.GetByIdAsync(patientId, ct);
        if (patient is null || !patient.IsActive || patient.CareStatus != PatientCareStatus.InFollowUp) return null;
        cache[patientId] = patient.FullName;
        return patient.FullName;
    }
}
