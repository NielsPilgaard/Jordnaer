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
	private int _refCount;

	/// <summary>
	/// Increments the reference count and starts the connection on the first acquire.
	/// </summary>
	public async Task AcquireAsync(CancellationToken cancellationToken = default)
	{
		if (Interlocked.Increment(ref _refCount) == 1)
		{
			await StartAsync(cancellationToken);
		}
	}

	/// <summary>
	/// Decrements the reference count and stops the connection when the last consumer releases it.
	/// </summary>
	public async Task ReleaseAsync(CancellationToken cancellationToken = default)
	{
		if (Interlocked.Decrement(ref _refCount) <= 0)
		{
			_refCount = 0;
			await StopAsync(cancellationToken);
		}
	}

	public IDisposable? OnNotificationReceived(Func<NotificationDto, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.ReceiveNotification), action);
	}

	public IDisposable? OnNotificationRead(Func<Guid, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.NotificationRead), action);
	}

	public IDisposable? OnNotificationsCleared(Func<string, string?, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.NotificationsCleared), action);
	}

	public IDisposable? OnUnreadCountChanged(Func<int, Task> action)
	{
		if (HubConnection is null)
		{
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.UnreadCountChanged), action);
	}
}
