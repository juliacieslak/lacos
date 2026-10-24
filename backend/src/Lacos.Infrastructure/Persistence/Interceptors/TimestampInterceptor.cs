using Lacos.Application.Interfaces;
using Lacos.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lacos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Preenche automaticamente created_at e updated_at de todas as entidades (modelo de dados).
/// Centralizar aqui evita que algum caso de uso esqueça de atualizar os carimbos.
/// </summary>
public sealed class TimestampInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _clock;

    public TimestampInterceptor(IDateTimeProvider clock) => _clock = clock;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;
        var now = _clock.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.CreatedAt).IsModified = false; // created_at é imutável.
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
