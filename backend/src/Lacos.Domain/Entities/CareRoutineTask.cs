using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Tarefa planejada da rotina de cuidado (tabela <c>care_routine_tasks</c>).
///
/// Origem: WF01 ("Agenda de hoje" com checkboxes de cuidados concluídos/pendentes, botão "+ Adicionar cuidado"),
/// WF03 (itens como "Almoço e hidratação", "Verificar sinais vitais", "Fisioterapia"), menu "Rotina de cuidado",
/// validação com stakeholders (marcar cuidados como realizados) e a preferência <c>care_routine_checkin</c>
/// ("lembretes periódicos para a realização de rotinas de cuidado ou aferições").
/// SUPOSIÇÃO: a tarefa recorre diariamente no horário informado, ou apenas nos dias da semana selecionados.
/// </summary>
public class CareRoutineTask : PatientRecord
{
    private CareRoutineTask() { }

    public CareRoutineTask(Guid patientId, Guid createdByUserId, string title, string? description, CareDiaryCategory category,
        TimeOnly scheduledTime, IReadOnlyList<DayOfWeek> daysOfWeek, DateOnly startDate, DateOnly? endDate)
        : base(patientId, createdByUserId)
    {
        Apply(title, description, category, scheduledTime, daysOfWeek, startDate, endDate);
    }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CareDiaryCategory Category { get; private set; }
    public TimeOnly ScheduledTime { get; private set; }
    public List<DayOfWeek> DaysOfWeek { get; private set; } = new();
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public override string AuditEntityType => "CARE_ROUTINE_TASK";

    public void Update(string title, string? description, CareDiaryCategory category, TimeOnly scheduledTime,
        IReadOnlyList<DayOfWeek> daysOfWeek, DateOnly startDate, DateOnly? endDate, Guid userId)
    {
        Apply(title, description, category, scheduledTime, daysOfWeek, startDate, endDate);
        TouchBy(userId);
    }

    /// <summary>Indica se a tarefa ocorre na data informada.</summary>
    public bool OccursOn(DateOnly date) =>
        DeletedAt is null && date >= StartDate && (EndDate is null || date <= EndDate)
        && (DaysOfWeek.Count == 0 || DaysOfWeek.Contains(date.DayOfWeek));

    private void Apply(string title, string? description, CareDiaryCategory category, TimeOnly scheduledTime,
        IReadOnlyList<DayOfWeek> daysOfWeek, DateOnly startDate, DateOnly? endDate)
    {
        if (endDate.HasValue && endDate < startDate)
            throw new BusinessRuleException("INVALID_ROUTINE_PERIOD", "A data de fim não pode ser anterior à de início.");
        Title = title.Trim();
        Description = description?.Trim();
        Category = category;
        ScheduledTime = scheduledTime;
        DaysOfWeek = daysOfWeek.Distinct().OrderBy(d => d).ToList();
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>
/// Conclusão de uma tarefa da rotina em uma data (tabela <c>care_routine_completions</c>).
/// Regra: no máximo uma conclusão por tarefa e data (índice único). Desmarcar = exclusão lógica.
/// </summary>
public class CareRoutineCompletion : PatientRecord
{
    private CareRoutineCompletion() { }

    public CareRoutineCompletion(Guid patientId, Guid taskId, Guid completedByUserId, DateOnly scheduledDate,
        DateTimeOffset completedAt, string? notes) : base(patientId, completedByUserId)
    {
        TaskId = taskId;
        ScheduledDate = scheduledDate;
        CompletedAt = completedAt;
        Notes = notes?.Trim();
    }

    public Guid TaskId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }
    [SensitiveData] public string? Notes { get; private set; }

    public CareRoutineTask? Task { get; private set; }

    public override string AuditEntityType => "CARE_ROUTINE_COMPLETION";
}
