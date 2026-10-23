using Lacos.Application.Interfaces;
using Lacos.Application.Interfaces.Integrations;
using Lacos.Application.Options;
using Lacos.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lacos.Application.Services;

/// <summary>
/// Entrega das notificações pendentes pelos canais externos (e-mail e push) — RNF11.
/// Executado continuamente pelo worker; cada notificação tem até 5 tentativas antes de ser marcada como FAILED.
/// </summary>
public interface INotificationDispatchService
{
    Task<int> DispatchPendingAsync(CancellationToken ct = default);
}

public sealed class NotificationDispatchService : INotificationDispatchService
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IEmailSender _email;
    private readonly IPushNotificationSender _push;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly NotificationChannelOptions _options;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(INotificationRepository notifications, IUserRepository users, IEmailSender email,
        IPushNotificationSender push, IUnitOfWork unitOfWork, IDateTimeProvider clock, IOptions<NotificationChannelOptions> options,
        ILogger<NotificationDispatchService> logger)
    {
        _notifications = notifications;
        _users = users;
        _email = email;
        _push = push;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Fluxo: busca um lote de pendentes (mais antigas primeiro) → envia por cada canal habilitado →
    /// marca como enviada, ou registra a falha para nova tentativa na próxima rodada.
    /// Usuários inativos/anonimizados não recebem envios externos.
    /// </summary>
    public async Task<int> DispatchPendingAsync(CancellationToken ct = default)
    {
        var pending = await _notifications.ListPendingDeliveryAsync(_options.BatchSize, ct);
        if (pending.Count == 0) return 0;

        var sent = 0;
        foreach (var notification in pending)
        {
            try
            {
                var user = await _users.GetByIdAsync(notification.UserId, ct);
                if (user is { IsActive: true })
                {
                    if (_options.EmailEnabled)
                        await _email.SendAsync(user.Email, user.Name, notification.Title,
                            $"<p>{System.Net.WebUtility.HtmlEncode(notification.Message)}</p>", ct);
                    if (_options.PushEnabled)
                        await _push.SendAsync(user.Id, notification.Title, notification.Message, new Dictionary<string, string>
                        {
                            ["notificationId"] = notification.Id.ToString(),
                            ["category"] = notification.Category.ToString(),
                            ["patientId"] = notification.PatientId?.ToString() ?? string.Empty
                        }, ct);
                }
                notification.MarkSent(_clock.UtcNow);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Falha ao entregar a notificação {NotificationId}", notification.Id);
                notification.RegisterFailure(ex.Message);
            }
        }
        await _unitOfWork.SaveChangesAsync(ct);
        return sent;
    }
}
