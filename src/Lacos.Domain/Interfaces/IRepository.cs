using Lacos.Domain.Common;

namespace Lacos.Domain.Interfaces;

/// <summary>
/// Contrato genérico do Repository Pattern: operações básicas de persistência por agregado.
/// A confirmação da transação é responsabilidade do <see cref="IUnitOfWork"/>, permitindo que um caso de uso
/// altere vários agregados e grave tudo (inclusive a auditoria) de forma atômica.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
}

/// <summary>
/// Repositório para registros clínicos vinculados a paciente (soft delete aplicado por filtro global).
/// </summary>
public interface IPatientRecordRepository<T> : IRepository<T> where T : PatientRecord
{
    /// <summary>Obtém um registro garantindo que pertença ao paciente informado (evita IDOR).</summary>
    Task<T?> GetForPatientAsync(Guid patientId, Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lista os registros do paciente no intervalo (pela data de referência do tipo), em ordem decrescente, paginados.
    /// </summary>
    Task<(IReadOnlyList<T> Items, int Total)> ListByPatientAsync(Guid patientId, DateTimeOffset? from, DateTimeOffset? to,
        int skip, int take, CancellationToken ct = default);
}

/// <summary>Unidade de trabalho: confirma todas as alterações pendentes em uma única transação.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
