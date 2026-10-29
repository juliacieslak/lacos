using Lacos.Domain.Common;
using Lacos.Domain.Enums;

namespace Lacos.Domain.Entities;

/// <summary>
/// Notificação destinada a um usuário (tabela <c>notifications</c>).
///
/// Funciona como caixa de entrada (notificações in-app exibidas no sino do header - WF01/WF02/WF03)
/// e como fila de saída ("outbox") para os canais externos (e-mail/push).
/// RNF11: um worker processa a fila continuamente para entrega em até 1 minuto.
/// <see cref="DeduplicationKey"/> é único e garante que lembretes automáticos não sejam duplicados
/// mesmo com múltiplas instâncias da API ou reprocessamentos.
/// </summary>
public class Notification : BaseEntity
{
    public const int MaxDeliveryAttempts = 5;

    private Notification() { }

    public Notification(Guid userId, Guid? patientId, NotificationCategory category, string title, string message,
        string deduplicationKey, Guid? relatedEntityId = null)
    {
        UserId = userId;
        PatientId = patientId;
        Category = category;
        Title = title.Length > 150 ? title[..150] : title;
        Message = message;
        DeduplicationKey = deduplicationKey;
        RelatedEntityId = relatedEntityId;
        Status = NotificationStatus.Pending;
    }

    public Guid UserId { get; private set; }
    public Guid? PatientId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public Guid? RelatedEntityId { get; private set; }
    public string DeduplicationKey { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>Marca como entregue pelos canais externos.</summary>
    public void MarkSent(DateTimeOffset when)
    {
        Status = NotificationStatus.Sent;
        SentAt = when;
        LastError = null;
    }

    /// <summary>
    /// Registra falha de entrega. Após <see cref="MaxDeliveryAttempts"/> tentativas a notificação é marcada como FAILED
    /// (continua visível in-app; apenas o envio externo é abandonado).
    /// </summary>
    public void RegisterFailure(string error)
    {
        Attempts++;
        LastError = error.Length > 1000 ? error[..1000] : error;
        if (Attempts >= MaxDeliveryAttempts) Status = NotificationStatus.Failed;
    }

    /// <summary>Marca como lida pelo usuário.</summary>
    public void MarkRead(DateTimeOffset when) => ReadAt ??= when;
}
