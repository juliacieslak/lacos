using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Interfaces;
using Lacos.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Lacos.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(LacosDbContext context) : base(context) { }

    public Task<User?> GetByAuth0IdAsync(string auth0UserId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> ExistsByCpfHashAsync(string cpfHash, Guid? exceptUserId, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.CpfHash == cpfHash && (exceptUserId == null || u.Id != exceptUserId), ct);

    /// <summary>Busca nomes em lote (uma consulta) para exibir os responsáveis pelos registros.</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        return await Set.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
    }
}

public sealed class UserConsentRepository : Repository<UserConsent>, IUserConsentRepository
{
    public UserConsentRepository(LacosDbContext context) : base(context) { }

    public async Task<IReadOnlyList<UserConsent>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
        await Set.Where(c => c.UserId == userId).ToListAsync(ct);
}

public sealed class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(LacosDbContext context) : base(context) { }

    public Task<bool> ExistsByCpfHashAsync(string cpfHash, Guid? exceptPatientId, CancellationToken ct = default) =>
        Set.AnyAsync(p => p.CpfHash == cpfHash && (exceptPatientId == null || p.Id != exceptPatientId), ct);

    /// <summary>
    /// Pacientes visíveis ao usuário: vínculo ativo + can_view_patient + paciente não excluído (RF11).
    /// Busca por nome sem diferenciar maiúsculas/minúsculas (ILIKE no PostgreSQL).
    /// </summary>
    public async Task<IReadOnlyList<(Patient Patient, PatientUserPermission Permission)>> ListAccessibleAsync(Guid userId, string? search,
        PatientCareStatus? status, CancellationToken ct = default)
    {
        var query = Context.PatientUserPermissions
            .Include(p => p.Patient)
            .Where(p => p.UserId == userId && p.IsActive && p.CanViewPatient && p.Patient!.ErasedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = Context.Database.IsNpgsql()
                ? query.Where(p => EF.Functions.ILike(p.Patient!.FullName, pattern))
                : query.Where(p => p.Patient!.FullName.ToLower().Contains(search.ToLower()));
        }
        if (status.HasValue) query = query.Where(p => p.Patient!.CareStatus == status);

        var list = await query.OrderBy(p => p.Patient!.FullName).ToListAsync(ct);
        return list.Select(p => (p.Patient!, p)).ToList();
    }
}

public sealed class PatientPermissionRepository : Repository<PatientUserPermission>, IPatientPermissionRepository
{
    public PatientPermissionRepository(LacosDbContext context) : base(context) { }

    public Task<PatientUserPermission?> GetAsync(Guid patientId, Guid userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.PatientId == patientId && p.UserId == userId, ct);

    public async Task<IReadOnlyList<PatientUserPermission>> ListByPatientAsync(Guid patientId, bool includeInactive, CancellationToken ct = default) =>
        await Set.Include(p => p.User)
            .Where(p => p.PatientId == patientId && (includeInactive || p.IsActive))
            .OrderByDescending(p => p.CanManageMembers).ThenBy(p => p.User!.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PatientUserPermission>> ListActiveByUserAsync(Guid userId, CancellationToken ct = default) =>
        await Set.Include(p => p.Patient).Where(p => p.UserId == userId && p.IsActive && p.Patient!.ErasedAt == null).ToListAsync(ct);

    public Task<int> CountActiveManagersAsync(Guid patientId, CancellationToken ct = default) =>
        Set.CountAsync(p => p.PatientId == patientId && p.IsActive && p.CanManageMembers, ct);
}

public sealed class PatientInvitationRepository : Repository<PatientInvitation>, IPatientInvitationRepository
{
    public PatientInvitationRepository(LacosDbContext context) : base(context) { }

    public override Task<PatientInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(i => i.Patient).FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<PatientInvitation>> ListPendingByEmailAsync(string email, DateTimeOffset now, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await Set.Include(i => i.Patient)
            .Where(i => i.InvitedEmail == normalized && i.Status == InvitationStatus.Pending && i.ExpiresAt > now && i.Patient!.ErasedAt == null)
            .OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PatientInvitation>> ListByPatientAsync(Guid patientId, CancellationToken ct = default) =>
        await Set.Include(i => i.Patient).Where(i => i.PatientId == patientId).OrderByDescending(i => i.CreatedAt).ToListAsync(ct);

    public Task<bool> ExistsPendingAsync(Guid patientId, string email, DateTimeOffset now, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.AnyAsync(i => i.PatientId == patientId && i.InvitedEmail == normalized && i.Status == InvitationStatus.Pending && i.ExpiresAt > now, ct);
    }
}

public sealed class NotificationPreferenceRepository : Repository<NotificationPreference>, INotificationPreferenceRepository
{
    public NotificationPreferenceRepository(LacosDbContext context) : base(context) { }

    public Task<NotificationPreference?> GetAsync(Guid patientId, Guid userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.PatientId == patientId && p.UserId == userId, ct);

    public async Task<IReadOnlyList<NotificationPreference>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
        await Set.Where(p => p.UserId == userId).ToListAsync(ct);

    /// <summary>
    /// Destinatários: junção preferências x vínculos ativos com permissão de consulta, filtrando a categoria.
    /// O filtro por categoria é traduzido para SQL (sem carregar todas as preferências em memória).
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ListRecipientsAsync(Guid patientId, NotificationCategory category, CancellationToken ct = default)
    {
        var prefs = Set.Where(p => p.PatientId == patientId && p.IsActive);
        prefs = category switch
        {
            NotificationCategory.MedicationReminder => prefs.Where(p => p.MedicationReminder),
            NotificationCategory.MedicationMissed => prefs.Where(p => p.MedicationMissed),
            NotificationCategory.AppointmentReminder => prefs.Where(p => p.AppointmentReminder),
            NotificationCategory.CareRoutineCheckin => prefs.Where(p => p.CareRoutineCheckin),
            NotificationCategory.PatientDataAdded => prefs.Where(p => p.PatientDataAdded),
            NotificationCategory.HealthThresholdAlert => prefs.Where(p => p.HealthThresholdAlert),
            _ => prefs.Where(_ => false)
        };
        return await prefs
            .Join(Context.PatientUserPermissions.Where(l => l.PatientId == patientId && l.IsActive && l.CanViewPatient),
                p => p.UserId, l => l.UserId, (p, l) => p.UserId)
            .Distinct().ToListAsync(ct);
    }
}

public sealed class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(LacosDbContext context) : base(context) { }

    // Considera também as notificações adicionadas nesta unidade de trabalho e ainda não gravadas.
    public async Task<bool> ExistsByDeduplicationKeyAsync(string key, CancellationToken ct = default) =>
        Set.Local.Any(n => n.DeduplicationKey == key) || await Set.AnyAsync(n => n.DeduplicationKey == key, ct);

    public async Task<(IReadOnlyList<Notification> Items, int Total, int Unread)> ListByUserAsync(Guid userId, bool unreadOnly, int skip, int take,
        CancellationToken ct = default)
    {
        var query = Set.AsNoTracking().Where(n => n.UserId == userId);
        var unread = await query.CountAsync(n => n.ReadAt == null, ct);
        if (unreadOnly) query = query.Where(n => n.ReadAt == null);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total, unread);
    }

    public async Task<IReadOnlyList<Notification>> ListPendingDeliveryAsync(int take, CancellationToken ct = default) =>
        await Set.Where(n => n.Status == NotificationStatus.Pending).OrderBy(n => n.CreatedAt).Take(take).ToListAsync(ct);

    public async Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset when, CancellationToken ct = default)
    {
        var unread = await Set.Where(n => n.UserId == userId && n.ReadAt == null).ToListAsync(ct);
        foreach (var n in unread) n.MarkRead(when);
        await Context.SaveChangesAsync(ct);
        return unread.Count;
    }

    public async Task DeleteByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var all = await Set.Where(n => n.UserId == userId).ToListAsync(ct);
        Set.RemoveRange(all);
    }
}

/// <summary>Trilha de auditoria: somente inclusão e consulta (RNF04 - imutável).</summary>
public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly LacosDbContext _context;

    public AuditLogRepository(LacosDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default) => await _context.AuditLogs.AddAsync(log, ct);

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> ListByPatientAsync(Guid patientId, string? entityType, Guid? entityId,
        AuditAction? action, DateTimeOffset? from, DateTimeOffset? to, bool includeViews, int skip, int take, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().Where(l => l.PatientId == patientId);
        if (!includeViews && action != AuditAction.View) query = query.Where(l => l.Action != AuditAction.View);
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(l => l.EntityType == entityType);
        if (entityId.HasValue) query = query.Where(l => l.EntityId == entityId);
        if (action.HasValue) query = query.Where(l => l.Action == action);
        if (from.HasValue) query = query.Where(l => l.CreatedAt >= from);
        if (to.HasValue) query = query.Where(l => l.CreatedAt < to);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }
}

public sealed class HealthThresholdRepository : Repository<HealthThreshold>, IHealthThresholdRepository
{
    public HealthThresholdRepository(LacosDbContext context) : base(context) { }

    public Task<HealthThreshold?> GetByPatientAsync(Guid patientId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(t => t.PatientId == patientId, ct);
}
