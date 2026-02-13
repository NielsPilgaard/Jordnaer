using Jordnaer.Features.Authentication;
using Jordnaer.Shared.Notifications;
using Jordnaer.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Features.Notifications;

public class NotificationSignalRClient(
	CurrentUser currentUser,
	ILogger<AuthenticatedSignalRClientBase> logger,
	NavigationManager navigationManager)
	: AuthenticatedSignalRClientBase(logger, currentUser, navigationManager, "/hubs/notifications")
{
	public void OnNotificationReceived(Func<NotificationDto, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(INotificationHub.ReceiveNotification));
		HubConnection.On(nameof(INotificationHub.ReceiveNotification), action);
	}

	public void OnNotificationRead(Func<Guid, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(INotificationHub.NotificationRead));
		HubConnection.On(nameof(INotificationHub.NotificationRead), action);
	}

	public void OnNotificationsCleared(Func<string, string?, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(INotificationHub.NotificationsCleared));
		HubConnection.On(nameof(INotificationHub.NotificationsCleared), action);
	}

	public void OnUnreadCountChanged(Func<int, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(INotificationHub.UnreadCountChanged));
		HubConnection.On(nameof(INotificationHub.UnreadCountChanged), action);
	}
}
