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
	public IDisposable? OnNotificationReceived(Func<NotificationDto, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		HubConnection.Remove(nameof(INotificationHub.ReceiveNotification));
		return HubConnection.On(nameof(INotificationHub.ReceiveNotification), action);
	}

	public IDisposable? OnNotificationRead(Func<Guid, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		HubConnection.Remove(nameof(INotificationHub.NotificationRead));
		return HubConnection.On(nameof(INotificationHub.NotificationRead), action);
	}

	public IDisposable? OnNotificationsCleared(Func<string, string?, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		HubConnection.Remove(nameof(INotificationHub.NotificationsCleared));
		return HubConnection.On(nameof(INotificationHub.NotificationsCleared), action);
	}

	public IDisposable? OnUnreadCountChanged(Func<int, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		HubConnection.Remove(nameof(INotificationHub.UnreadCountChanged));
		return HubConnection.On(nameof(INotificationHub.UnreadCountChanged), action);
	}
}
