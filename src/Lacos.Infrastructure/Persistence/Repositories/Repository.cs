using System.Linq.Expressions;
using Lacos.Domain.Common;
using Lacos.Domain.Entities;
using Lacos.Domain.Interfaces;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Lacos.Infrastructure.Persistence.Repositories;

/// <summary>Implementação genérica do Repository Pattern sobre o EF Core.</summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected Repository(LacosDbContext context) => Context = context;

    protected LacosDbContext Context { get; }
    protected DbSet<T> Set => Context.Set<T>();

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);

    public virtual void Remove(T entity) => Set.Remove(entity);
}

/// <summary>
/// Repositório genérico de registros clínicos. A data de referência usada no filtro/ordenação do período
/// é definida por tipo (<see cref="ReferenceDate"/>), ex.: data da aferição, da ocorrência ou do agendamento.
/// O filtro global do DbContext já exclui registros com exclusão lógica.
/// </summary>
public class PatientRecordRepository<T> : Repository<T>, IPatientRecordRepository<T> where T : PatientRecord
{
    public PatientRecordRepository(LacosDbContext context) : base(context) { }

    /// <summary>Data de referência do registro (padrão: data de criação).</summary>
    protected virtual Expression<Func<T, DateTimeOffset>> ReferenceDate => e => e.CreatedAt;

    /// <summary>Consulta base (permite incluir navegações nas subclasses).</summary>
    protected virtual IQueryable<T> Query => Set;

    public Task<T?> GetForPatientAsync(Guid patientId, Guid id, CancellationToken ct = default) =>
        Query.FirstOrDefaultAsync(e => e.Id == id && e.PatientId == patientId, ct);

    /// <summary>
    /// Filtra por paciente e período (from inclusivo, to exclusivo) e ordena por data decrescente (US04).
    /// O total é calculado na mesma consulta lógica para a paginação.
    /// </summary>
    public async Task<(IReadOnlyList<T> Items, int Total)> ListByPatientAsync(Guid patientId, DateTimeOffset? from, DateTimeOffset? to,
        int skip, int take, CancellationToken ct = default)
    {
        var query = Query.AsNoTracking().Where(e => e.PatientId == patientId);
        if (from.HasValue) query = query.Where(Compare(ReferenceDate, from.Value, greaterOrEqual: true));
        if (to.HasValue) query = query.Where(Compare(ReferenceDate, to.Value, greaterOrEqual: false));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(ReferenceDate).ThenByDescending(e => e.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    // Constrói "e => selector(e) >= value" ou "e => selector(e) < value" preservando a tradução para SQL.
    private static Expression<Func<T, bool>> Compare(Expression<Func<T, DateTimeOffset>> selector, DateTimeOffset value, bool greaterOrEqual)
    {
        var constant = Expression.Constant(value);
        Expression body = greaterOrEqual
            ? Expression.GreaterThanOrEqual(selector.Body, constant)
            : Expression.LessThan(selector.Body, constant);
        return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
    }
}

public sealed class SymptomRecordRepository : PatientRecordRepository<SymptomRecord>
{
    public SymptomRecordRepository(LacosDbContext context) : base(context) { }
    protected override Expression<Func<SymptomRecord, DateTimeOffset>> ReferenceDate => e => e.OccurredAt;
}

public sealed class CareDiaryRepository : PatientRecordRepository<CareDiaryEntry>
{
    public CareDiaryRepository(LacosDbContext context) : base(context) { }
    protected override Expression<Func<CareDiaryEntry, DateTimeOffset>> ReferenceDate => e => e.OccurredAt;
}
