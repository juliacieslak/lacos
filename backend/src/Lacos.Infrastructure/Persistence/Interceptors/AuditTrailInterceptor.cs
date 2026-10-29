using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lacos.Application.Interfaces;
using Lacos.Domain.Common;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lacos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Trilha de auditoria automática (RF06, RF10, RNF04).
///
/// Para cada entidade <see cref="IAuditable"/> incluída, alterada ou excluída, grava um registro em audit_logs
/// NA MESMA TRANSAÇÃO da alteração — se a gravação do dado falhar, a auditoria também não é gravada, e vice-versa.
///
/// Algoritmo:
/// 1. Coleta as entradas do ChangeTracker em estado Added/Modified/Deleted.
/// 2. Determina a ação: Added → CREATE; Deleted ou DeletedAt preenchido → DELETE;
///    alteração de Status/CareStatus/IsActive → STATUS_CHANGE; demais → UPDATE.
/// 3. Monta old_values/new_values apenas com as propriedades alteradas (na inclusão, todos os valores).
///    Propriedades [SensitiveData] têm o valor criptografado dentro do JSON (RNF02).
/// 4. Identifica o usuário responsável pelo "sub" do token (RF10) e o IP/User-Agent da requisição (RNF04).
/// Exclusões definitivas (LGPD) não registram os valores apagados: o serviço grava uma ação ERASURE explícita.
/// </summary>
public sealed class AuditTrailInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredProperties = new()
    {
        nameof(BaseEntity.CreatedAt), nameof(BaseEntity.UpdatedAt), "CpfHash"
    };

    private static readonly HashSet<string> StatusProperties = new() { "Status", "CareStatus", "IsActive" };
    private static readonly HashSet<string> ErasureMarkers = new() { nameof(Patient.ErasedAt), nameof(User.AnonymizedAt) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
    };

    private readonly ICurrentUser _currentUser;
    private readonly IEncryptionService _encryption;
    private readonly IDateTimeProvider _clock;

    public AuditTrailInterceptor(ICurrentUser currentUser, IEncryptionService encryption, IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _encryption = encryption;
        _clock = clock;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) await AddAuditEntriesAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) AddAuditEntriesAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    private async Task AddAuditEntriesAsync(DbContext context, CancellationToken ct)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (entries.Count == 0) return;

        var actorId = await ResolveActorAsync(context, ct);
        var now = _clock.UtcNow;

        foreach (var entry in entries)
        {
            var auditable = (IAuditable)entry.Entity;
            if (entry.State == EntityState.Modified && entry.Properties.Any(p => p.IsModified && ErasureMarkers.Contains(p.Metadata.Name)))
                continue;
            if (entry.State == EntityState.Deleted && ErasureScope.IsErased(context, entry.Entity))
                continue;

            var changed = entry.Properties
                .Where(p => !IgnoredProperties.Contains(p.Metadata.Name) && !p.Metadata.IsShadowProperty())
                .Where(p => entry.State != EntityState.Modified || (p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)))
                .ToList();
            if (entry.State == EntityState.Modified && changed.Count == 0) continue;

            var action = ResolveAction(entry, changed);
            var oldValues = entry.State == EntityState.Added ? null : Serialize(changed, p => p.OriginalValue);
            var newValues = entry.State == EntityState.Deleted ? null : Serialize(changed, p => p.CurrentValue);
            var entityId = entry.Property(nameof(BaseEntity.Id)).CurrentValue as Guid?;

            // No primeiro acesso, o próprio usuário recém-criado é o responsável pelo seu registro.
            var userId = actorId ?? (entry.Entity is User && entry.State == EntityState.Added ? entityId : null);

            context.Set<AuditLog>().Add(new AuditLog(userId, auditable.AuditPatientId, auditable.AuditEntityType, entityId, action,
                oldValues, newValues, null, _currentUser.IpAddress, _currentUser.UserAgent, now));
        }
    }

    private static AuditAction ResolveAction(EntityEntry entry, IReadOnlyCollection<PropertyEntry> changed)
    {
        if (entry.State == EntityState.Added) return AuditAction.Create;
        if (entry.State == EntityState.Deleted) return AuditAction.Delete;
        if (changed.Any(p => p.Metadata.Name == nameof(ISoftDeletable.DeletedAt) && p.OriginalValue is null && p.CurrentValue is not null))
            return AuditAction.Delete;
        if (changed.Any(p => StatusProperties.Contains(p.Metadata.Name))) return AuditAction.StatusChange;
        return AuditAction.Update;
    }

    private string Serialize(IEnumerable<PropertyEntry> properties, Func<PropertyEntry, object?> selector)
    {
        var values = new Dictionary<string, object?>();
        foreach (var p in properties)
        {
            var value = selector(p);
            var sensitive = p.Metadata.PropertyInfo?.GetCustomAttribute<SensitiveDataAttribute>() is not null;
            values[p.Metadata.Name] = sensitive && value is string s ? _encryption.Encrypt(s) : value;
        }
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    /// <summary>
    /// Resolve o id interno do usuário autenticado. Primeiro procura entre as entidades rastreadas (sem ida ao banco);
    /// depois consulta pelo auth0_user_id. Retorna null para ações do sistema (workers sem usuário).
    /// </summary>
    private async Task<Guid?> ResolveActorAsync(DbContext context, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.Auth0UserId)) return null;
        var tracked = context.ChangeTracker.Entries<User>().FirstOrDefault(u => u.Entity.Auth0UserId == _currentUser.Auth0UserId);
        if (tracked is not null) return tracked.State == EntityState.Added ? null : tracked.Entity.Id;
        var id = await context.Set<User>().AsNoTracking().Where(u => u.Auth0UserId == _currentUser.Auth0UserId)
            .Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
        return id;
    }
}
