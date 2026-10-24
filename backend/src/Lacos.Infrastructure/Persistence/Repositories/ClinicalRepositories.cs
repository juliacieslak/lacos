using System.Linq.Expressions;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Lacos.Infrastructure.Persistence.Repositories;

public sealed class PatientEventRepository : PatientRecordRepository<PatientEvent>, IPatientEventRepository
{
    public PatientEventRepository(LacosDbContext context) : base(context) { }

    protected override Expression<Func<PatientEvent, DateTimeOffset>> ReferenceDate => e => e.ScheduledAt;

    public async Task<IReadOnlyList<PatientEvent>> ListInRangeAsync(IEnumerable<Guid> patientIds, DateTimeOffset from, DateTimeOffset to,
        PatientEventType? type, PatientEventStatus? status, CancellationToken ct = default)
    {
        var ids = patientIds.ToList();
        var query = Set.AsNoTracking().Where(e => ids.Contains(e.PatientId) && e.ScheduledAt >= from && e.ScheduledAt < to);
        if (type.HasValue) query = query.Where(e => e.EventType == type);
        if (status.HasValue) query = query.Where(e => e.Status == status);
        return await query.OrderBy(e => e.ScheduledAt).ToListAsync(ct);
    }

    public Task<PatientEvent?> GetNextScheduledAsync(Guid patientId, DateTimeOffset from, CancellationToken ct = default) =>
        Set.AsNoTracking().Where(e => e.PatientId == patientId && e.Status == PatientEventStatus.Scheduled && e.ScheduledAt >= from)
            .OrderBy(e => e.ScheduledAt).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PatientEvent>> ListScheduledBetweenAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(e => e.Status == PatientEventStatus.Scheduled && e.ScheduledAt > from && e.ScheduledAt <= to)
            .Join(Context.Patients.Where(p => p.IsActive), e => e.PatientId, p => p.Id, (e, p) => e)
            .ToListAsync(ct);
}

public sealed class MedicationRepository : PatientRecordRepository<Medication>, IMedicationRepository
{
    public MedicationRepository(LacosDbContext context) : base(context) { }

    /// <summary>
    /// Medicamentos dos pacientes. Com activeOnly, restringe pelo período de uso no banco;
    /// o filtro de dias da semana é aplicado em memória (regra de domínio <see cref="Medication.IsActiveOn"/>).
    /// </summary>
    public async Task<IReadOnlyList<Medication>> ListByPatientsAsync(IEnumerable<Guid> patientIds, bool activeOnly, DateOnly referenceDate,
        CancellationToken ct = default)
    {
        var ids = patientIds.ToList();
        var query = Set.AsNoTracking().Where(m => ids.Contains(m.PatientId));
        if (activeOnly) query = query.Where(m => m.StartDate <= referenceDate && (m.EndDate == null || m.EndDate >= referenceDate));
        var list = await query.OrderBy(m => m.Name).ToListAsync(ct);
        return activeOnly ? list.Where(m => m.IsActiveOn(referenceDate)).ToList() : list;
    }

    public async Task<IReadOnlyList<Medication>> ListActiveOnAsync(DateOnly date, CancellationToken ct = default)
    {
        var list = await Set.AsNoTracking()
            .Where(m => m.StartDate <= date && (m.EndDate == null || m.EndDate >= date) && m.AdministrationTimes.Count > 0)
            .Join(Context.Patients.Where(p => p.IsActive), m => m.PatientId, p => p.Id, (m, p) => m)
            .ToListAsync(ct);
        return list.Where(m => m.IsActiveOn(date)).ToList();
    }
}

public sealed class MedicationAdministrationRepository : PatientRecordRepository<MedicationAdministration>, IMedicationAdministrationRepository
{
    public MedicationAdministrationRepository(LacosDbContext context) : base(context) { }

    protected override Expression<Func<MedicationAdministration, DateTimeOffset>> ReferenceDate => e => e.AdministeredAt;
    protected override IQueryable<MedicationAdministration> Query => Set.Include(a => a.Medication);

    public Task<bool> ExistsForOccurrenceAsync(Guid medicationId, DateTimeOffset scheduledFor, CancellationToken ct = default) =>
        Set.AnyAsync(a => a.MedicationId == medicationId && a.ScheduledFor == scheduledFor, ct);

    public async Task<IReadOnlyList<MedicationAdministration>> ListByMedicationsAsync(IEnumerable<Guid> medicationIds, DateTimeOffset from,
        DateTimeOffset to, CancellationToken ct = default)
    {
        var ids = medicationIds.ToList();
        if (ids.Count == 0) return Array.Empty<MedicationAdministration>();
        return await Set.AsNoTracking()
            .Where(a => ids.Contains(a.MedicationId)
                        && ((a.ScheduledFor >= from && a.ScheduledFor < to) || (a.AdministeredAt >= from && a.AdministeredAt < to)))
            .ToListAsync(ct);
    }
}

public sealed class HealthMeasurementRepository : PatientRecordRepository<HealthMeasurement>, IHealthMeasurementRepository
{
    public HealthMeasurementRepository(LacosDbContext context) : base(context) { }

    protected override Expression<Func<HealthMeasurement, DateTimeOffset>> ReferenceDate => e => e.MeasuredAt;

    public async Task<IReadOnlyList<HealthMeasurement>> ListLatestAsync(Guid patientId, int take, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(m => m.PatientId == patientId).OrderByDescending(m => m.MeasuredAt).Take(take).ToListAsync(ct);
}

public sealed class HealthConditionRepository : PatientRecordRepository<HealthCondition>, IHealthConditionRepository
{
    public HealthConditionRepository(LacosDbContext context) : base(context) { }

    public async Task<IReadOnlyList<HealthCondition>> ListActiveByPatientsAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default)
    {
        var ids = patientIds.ToList();
        return await Set.AsNoTracking().Where(c => ids.Contains(c.PatientId) && c.Status == HealthConditionStatus.Active).ToListAsync(ct);
    }
}

public sealed class CareRoutineRepository : PatientRecordRepository<CareRoutineTask>, ICareRoutineRepository
{
    public CareRoutineRepository(LacosDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CareRoutineTask>> ListByPatientsAsync(IEnumerable<Guid> patientIds, CancellationToken ct = default)
    {
        var ids = patientIds.ToList();
        return await Set.AsNoTracking().Where(t => ids.Contains(t.PatientId)).OrderBy(t => t.ScheduledTime).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CareRoutineTask>> ListAllActiveAsync(DateOnly date, CancellationToken ct = default) =>
        await Set.AsNoTracking()
            .Where(t => t.StartDate <= date && (t.EndDate == null || t.EndDate >= date))
            .Join(Context.Patients.Where(p => p.IsActive), t => t.PatientId, p => p.Id, (t, p) => t)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CareRoutineCompletion>> ListCompletionsAsync(IEnumerable<Guid> taskIds, DateOnly from, DateOnly to,
        CancellationToken ct = default)
    {
        var ids = taskIds.ToList();
        if (ids.Count == 0) return Array.Empty<CareRoutineCompletion>();
        return await Context.CareRoutineCompletions.AsNoTracking()
            .Where(c => ids.Contains(c.TaskId) && c.ScheduledDate >= from && c.ScheduledDate <= to).ToListAsync(ct);
    }

    public Task<CareRoutineCompletion?> GetCompletionAsync(Guid taskId, DateOnly date, CancellationToken ct = default) =>
        Context.CareRoutineCompletions.FirstOrDefaultAsync(c => c.TaskId == taskId && c.ScheduledDate == date, ct);

    public async Task AddCompletionAsync(CareRoutineCompletion completion, CancellationToken ct = default) =>
        await Context.CareRoutineCompletions.AddAsync(completion, ct);
}
