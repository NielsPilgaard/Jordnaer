using Jordnaer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Features.Notifications;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub<INotificationHub>
{
	public override async Task OnConnectedAsync()
	{
		logger.LogDebug("User {userId} connected to {hubName}", Context.User?.GetId(), nameof(NotificationHub));
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (exception is not null)
		{
			logger.LogError(exception, "User {userId} disconnected from {hubName}. Exception message: {exceptionMessage}",
				Context.User?.GetId(), nameof(NotificationHub), exception.Message);
		}
		else
		{
			logger.LogDebug("User {userId} disconnected from {hubName}", Context.User?.GetId(), nameof(NotificationHub));
		}
		await base.OnDisconnectedAsync(exception);
	}
}
