using Jordnaer.Shared.Notifications;

namespace Jordnaer.Features.Notifications;

public interface INotificationHub
{
	Task ReceiveNotification(NotificationDto notification);
	Task NotificationRead(Guid notificationId);
	Task NotificationsCleared(string sourceType, string? sourceId);
	Task UnreadCountChanged(int totalUnread);
}
