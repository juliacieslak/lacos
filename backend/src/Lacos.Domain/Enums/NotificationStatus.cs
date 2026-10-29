namespace Lacos.Domain.Enums;

/// <summary>Situação de entrega de uma notificação (fila de saída / outbox).</summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}
