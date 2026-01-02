using Jordnaer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Features.Groups;

public interface IGroupMembershipHub
{
	Task MembershipStatusChanged(GroupMembershipStatusChanged notification);
}

[Authorize]
public class GroupMembershipHub(ILogger<GroupMembershipHub> logger) : Hub<IGroupMembershipHub>
{
	public override async Task OnConnectedAsync()
	{
		logger.LogDebug("User {userId} connected to {hubName}", Context.User?.GetId(), nameof(GroupMembershipHub));

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (exception is not null)
		{
			logger.LogError(exception, "User {userId} disconnected from {hubName}. " +
											"Exception message: {exceptionMessage}",
				Context.User?.GetId(), nameof(GroupMembershipHub), exception.Message);
		}
		else
		{
			logger.LogDebug("User {userId} disconnected from {hubName}", Context.User?.GetId(), nameof(GroupMembershipHub));
		}

		await base.OnDisconnectedAsync(exception);
	}
}
