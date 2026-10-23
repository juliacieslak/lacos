using Lacos.Domain.Common;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;

namespace Lacos.Domain.Entities;

/// <summary>
/// Medicamento utilizado pelo paciente (tabela <c>medications</c>) — RF08.
/// Campos: nome, dosagem, marca, motivo do uso, via de administração, profissional que prescreveu,
/// data de início e fim (se houver), frequência e horários de administração.
///
/// Os horários são interpretados no fuso horário configurado da aplicação (Scheduling:TimeZone).
/// SUPOSIÇÃO: a frequência é descrita em texto livre (ex.: "8 em 8 horas") e os horários concretos
/// são informados em <see cref="AdministrationTimes"/>; <see cref="DaysOfWeek"/> vazio significa todos os dias.
/// Medicamentos "se necessário" podem não possuir horários e, nesse caso, não geram lembretes.
/// </summary>
public class Medication : PatientRecord
{
    private Medication() { }

    public Medication(Guid patientId, Guid createdByUserId, MedicationData data) : base(patientId, createdByUserId)
    {
        Apply(data);
    }

    public string Name { get; private set; } = string.Empty;
    public string Dosage { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    [SensitiveData] public string? UsageReason { get; private set; }
    public AdministrationRoute AdministrationRoute { get; private set; }
    public string? PrescribedBy { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string Frequency { get; private set; } = string.Empty;
    public List<TimeOnly> AdministrationTimes { get; private set; } = new();
    public List<DayOfWeek> DaysOfWeek { get; private set; } = new();

    public override string AuditEntityType => "MEDICATION";

    public void Update(MedicationData data, Guid userId)
    {
        Apply(data);
        TouchBy(userId);
    }

    /// <summary>Encerra o uso do medicamento a partir de uma data (mantém histórico).</summary>
    public void Discontinue(DateOnly endDate, Guid userId)
    {
        if (endDate < StartDate)
            throw new BusinessRuleException("INVALID_MEDICATION_PERIOD", "A data de fim não pode ser anterior à de início.");
        EndDate = endDate;
        TouchBy(userId);
    }

    /// <summary>Indica se o medicamento está em uso na data informada.</summary>
    public bool IsActiveOn(DateOnly date) =>
        DeletedAt is null && date >= StartDate && (EndDate is null || date <= EndDate.Value)
        && (DaysOfWeek.Count == 0 || DaysOfWeek.Contains(date.DayOfWeek));

    /// <summary>
    /// Gera os horários programados (ocorrências) de uma data — usado pela agenda (WF03),
    /// pelos lembretes (medication_reminder) e pela detecção de doses não registradas (medication_missed).
    /// </summary>
    public IEnumerable<TimeOnly> GetScheduledTimes(DateOnly date) =>
        IsActiveOn(date) ? AdministrationTimes.OrderBy(t => t) : Enumerable.Empty<TimeOnly>();

    private void Apply(MedicationData d)
    {
        if (d.EndDate.HasValue && d.EndDate.Value < d.StartDate)
            throw new BusinessRuleException("INVALID_MEDICATION_PERIOD", "A data de fim não pode ser anterior à de início.");
        if (d.AdministrationTimes.Distinct().Count() != d.AdministrationTimes.Count)
            throw new BusinessRuleException("DUPLICATED_ADMINISTRATION_TIME", "Há horários de administração repetidos.");

        Name = d.Name.Trim();
        Dosage = d.Dosage.Trim();
        Brand = d.Brand?.Trim();
        UsageReason = d.UsageReason?.Trim();
        AdministrationRoute = d.AdministrationRoute;
        PrescribedBy = d.PrescribedBy?.Trim();
        StartDate = d.StartDate;
        EndDate = d.EndDate;
        Frequency = d.Frequency.Trim();
        AdministrationTimes = d.AdministrationTimes.OrderBy(t => t).ToList();
        DaysOfWeek = d.DaysOfWeek.Distinct().OrderBy(x => x).ToList();
    }
}

/// <summary>Dados de cadastro/edição de medicamento (evita construtores com muitos parâmetros).</summary>
public sealed record MedicationData(
    string Name,
    string Dosage,
    string? Brand,
    string? UsageReason,
    AdministrationRoute AdministrationRoute,
    string? PrescribedBy,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Frequency,
    IReadOnlyList<TimeOnly> AdministrationTimes,
    IReadOnlyList<DayOfWeek> DaysOfWeek);
