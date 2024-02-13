using Jordnaer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Features.Chat;

[Authorize]
public class ChatHub(ILogger<ChatHub> logger) : Hub<IChatHub>
{
	public override async Task OnConnectedAsync()
	{
		logger.LogDebug("User {userId} connected to {chatHub}", Context.User?.GetId(), nameof(ChatHub));

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (exception is not null)
		{
			logger.LogError(exception, "User {userId} disconnected from {chatHub}. " +
										"Exception message: {exceptionMessage}",
				Context.User?.GetId(), nameof(ChatHub), exception.Message);
		}
		else
		{
			logger.LogDebug("User {userId} disconnected from {chatHub}", Context.User?.GetId(), nameof(ChatHub));
		}

		await base.OnConnectedAsync();
	}
}