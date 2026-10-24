using Lacos.Application.Common;
using Lacos.Application.DTOs;
using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Application.Mappings;
using Lacos.Domain.Entities;
using Lacos.Domain.Enums;
using Lacos.Domain.Exceptions;
using Lacos.Domain.Interfaces;

namespace Lacos.Application.Services;

/// <summary>
/// Notificações (RF12, US02 "Notifica os usuários configurados para receber alertas sobre o evento", RNF11).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Enfileira uma notificação para todos os membros do paciente que aceitam a categoria.
    /// A notificação é adicionada à unidade de trabalho atual (gravada junto com o dado que a originou).
    /// </summary>
    Task<int> EnqueueForPatientAsync(Guid patientId, NotificationCategory category, string title, string message,
        string deduplicationPrefix, Guid? relatedEntityId, Guid? excludeUserId, CancellationToken ct = default);

    Task<NotificationListResponse> ListMineAsync(bool unreadOnly, PageQuery page, CancellationToken ct = default);
    Task MarkReadAsync(Guid notificationId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(CancellationToken ct = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationPreferenceRepository _preferences;
    private readonly ICurrentUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationDispatchSignal _signal;

    public NotificationService(INotificationRepository notifications, INotificationPreferenceRepository preferences,
        ICurrentUserContext userContext, IUnitOfWork unitOfWork, IDateTimeProvider clock, INotificationDispatchSignal signal)
    {
        _notifications = notifications;
        _preferences = preferences;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _signal = signal;
    }

    /// <summary>
    /// Algoritmo:
    /// 1. Busca destinatários (vínculo ativo + pode visualizar + preferência da categoria habilitada).
    /// 2. Remove o autor da ação (não faz sentido notificar quem acabou de registrar o dado).
    /// 3. Monta a chave de deduplicação por destinatário e ignora as já existentes (idempotência).
    /// 4. Sinaliza o worker de envio para entrega imediata (RNF11).
    /// </summary>
    public async Task<int> EnqueueForPatientAsync(Guid patientId, NotificationCategory category, string title, string message,
        string deduplicationPrefix, Guid? relatedEntityId, Guid? excludeUserId, CancellationToken ct = default)
    {
        var recipients = await _preferences.ListRecipientsAsync(patientId, category, ct);
        var count = 0;
        foreach (var userId in recipients.Where(id => id != excludeUserId))
        {
            var key = $"{deduplicationPrefix}:{userId:N}";
            if (await _notifications.ExistsByDeduplicationKeyAsync(key, ct)) continue;
            await _notifications.AddAsync(new Notification(userId, patientId, category, title, message, key, relatedEntityId), ct);
            count++;
        }
        if (count > 0) _signal.Signal();
        return count;
    }

    public async Task<NotificationListResponse> ListMineAsync(bool unreadOnly, PageQuery page, CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        var (items, total, unread) = await _notifications.ListByUserAsync(userId, unreadOnly, page.Skip, page.SafePageSize, ct);
        return new NotificationListResponse(
            new PagedResult<NotificationResponse>(items.Select(n => n.ToResponse()).ToList(), page.SafePage, page.SafePageSize, total),
            unread);
    }

    /// <summary>Marca como lida. Regra: um usuário só pode alterar as próprias notificações.</summary>
    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        var notification = await _notifications.GetByIdAsync(notificationId, ct);
        if (notification is null || notification.UserId != userId) throw new NotFoundException("Notificação", notificationId);
        notification.MarkRead(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<int> MarkAllReadAsync(CancellationToken ct = default)
    {
        var userId = await _userContext.GetRequiredUserIdAsync(ct);
        return await _notifications.MarkAllReadAsync(userId, _clock.UtcNow, ct);
    }
}
