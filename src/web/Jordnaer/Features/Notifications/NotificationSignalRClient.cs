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
	private readonly ILogger<AuthenticatedSignalRClientBase> _notificationLogger = logger;
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
		var newVal = Interlocked.Decrement(ref _refCount);
		if (newVal <= 0)
		{
			// Atomically reset to 0 only if the count is still <= 0, to avoid racing with AcquireAsync
			int current;
			do
			{
				current = Volatile.Read(ref _refCount);
				if (current > 0)
				{
					return;
				}
			} while (Interlocked.CompareExchange(ref _refCount, 0, current) != current);

			await StopAsync(cancellationToken);
		}
	}

	public IDisposable? OnNotificationReceived(Func<NotificationDto, Task> action)
	{
		if (HubConnection is null)
		{
			_notificationLogger.LogWarning(
				"Cannot register {SubscriptionName} handler: {HubConnectionName} is not available.",
				nameof(INotificationHub.ReceiveNotification), nameof(HubConnection));
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.ReceiveNotification), action);
	}

	public IDisposable? OnNotificationRead(Func<Guid, Task> action)
	{
		if (HubConnection is null)
		{
			_notificationLogger.LogWarning(
				"Cannot register {SubscriptionName} handler: {HubConnectionName} is not available.",
				nameof(INotificationHub.NotificationRead), nameof(HubConnection));
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.NotificationRead), action);
	}

	public IDisposable? OnNotificationsCleared(Func<string, string?, Task> action)
	{
		if (HubConnection is null)
		{
			_notificationLogger.LogWarning(
				"Cannot register {SubscriptionName} handler: {HubConnectionName} is not available.",
				nameof(INotificationHub.NotificationsCleared), nameof(HubConnection));
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.NotificationsCleared), action);
	}

	public IDisposable? OnUnreadCountChanged(Func<int, Task> action)
	{
		if (HubConnection is null)
		{
			_notificationLogger.LogWarning(
				"Cannot register {SubscriptionName} handler: {HubConnectionName} is not available.",
				nameof(INotificationHub.UnreadCountChanged), nameof(HubConnection));
			return null;
		}

		return HubConnection.On(nameof(INotificationHub.UnreadCountChanged), action);
	}
}
