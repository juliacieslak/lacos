using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Agenda de cuidados (WF03): visão por dia, semana ou mês de compromissos, medicações e tarefas da rotina,
/// de um paciente ou de todos os pacientes que o usuário acompanha (RF11).
/// </summary>
public interface IAgendaService
{
    Task<AgendaResponse> GetAsync(Guid? patientId, DateOnly? date, AgendaView view, CancellationToken ct = default);
}

public sealed class AgendaService : IAgendaService
{
    private readonly ICurrentUserContext _userContext;
    private readonly IPatientAccessService _access;
    private readonly IPatientRepository _patients;
    private readonly IAgendaBuilder _builder;
    private readonly IAuditService _audit;
    private readonly IDateTimeProvider _clock;

    public AgendaService(ICurrentUserContext userContext, IPatientAccessService access, IPatientRepository patients,
        IAgendaBuilder builder, IAuditService audit, IDateTimeProvider clock)
    {
        _userContext = userContext;
        _access = access;
        _patients = patients;
        _builder = builder;
        _audit = audit;
        _clock = clock;
    }

    /// <summary>
    /// Calcula o intervalo da visão: Dia = a data; Semana = segunda a domingo da semana da data; Mês = do dia 1 ao último dia.
    /// Somente pacientes que o usuário pode consultar são incluídos (RF05 - "Na Agenda: exibir apenas os dados ... autorizados").
    /// </summary>
    public async Task<AgendaResponse> GetAsync(Guid? patientId, DateOnly? date, AgendaView view, CancellationToken ct = default)
    {
        var reference = date ?? _clock.LocalToday;
        var (from, to) = view switch
        {
            AgendaView.Week => (reference.AddDays(-(((int)reference.DayOfWeek + 6) % 7)), reference.AddDays(6 - (((int)reference.DayOfWeek + 6) % 7))),
            AgendaView.Month => (new DateOnly(reference.Year, reference.Month, 1),
                new DateOnly(reference.Year, reference.Month, DateTime.DaysInMonth(reference.Year, reference.Month))),
            _ => (reference, reference)
        };

        Dictionary<Guid, string> patients;
        Guid userId;
        if (patientId is { } id)
        {
            var access = await _access.RequireAsync(id, PatientPermission.View, ct);
            userId = access.UserId;
            patients = new Dictionary<Guid, string> { [id] = access.Patient.FullName };
        }
        else
        {
            userId = await _userContext.GetRequiredUserIdAsync(ct);
            patients = (await _patients.ListAccessibleAsync(userId, null, null, ct)).ToDictionary(p => p.Patient.Id, p => p.Patient.FullName);
        }

        var items = await _builder.BuildAsync(patients, from, to, ct);
        foreach (var pid in patients.Keys) await _audit.RecordViewAsync(userId, pid, "AGENDA", null, ct);
        return new AgendaResponse(from, to, view, items);
    }
}
