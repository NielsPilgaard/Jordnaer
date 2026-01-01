using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Groups;

public interface IGroupMembershipHub
{
	Task MembershipStatusChanged(GroupMembershipStatusChanged notification);
}

[Authorize]
public class GroupMembershipHub(
	ILogger<GroupMembershipHub> logger,
	IDbContextFactory<JordnaerDbContext> contextFactory) : Hub<IGroupMembershipHub>
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

	public async Task JoinAdminGroups(List<Guid> groupIds)
	{
		var userId = Context.User?.GetId();
		if (userId is null)
		{
			logger.LogWarning("Unauthorized attempt to join admin groups - no user ID");
			return;
		}

		logger.LogDebug("User {userId} attempting to join {count} admin groups", userId, groupIds.Count);

		await using var context = await contextFactory.CreateDbContextAsync();

		// Validate that the user is actually an admin or owner of the requested groups
		var validGroupIds = await context.GroupMemberships
			.AsNoTracking()
			.Where(x => x.UserProfileId == userId &&
					   groupIds.Contains(x.GroupId) &&
					   (x.PermissionLevel == PermissionLevel.Admin ||
						x.OwnershipLevel == OwnershipLevel.Owner))
			.Select(x => x.GroupId)
			.ToListAsync();

		if (validGroupIds.Count != groupIds.Count)
		{
			var invalidGroupIds = groupIds.Except(validGroupIds).ToList();
			logger.LogWarning("User {userId} attempted to join admin groups without authorization. Invalid groups: {invalidGroupIds}",
				userId, invalidGroupIds);
		}

		foreach (var groupId in validGroupIds)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"group-admins-{groupId}");
		}

		logger.LogDebug("User {userId} joined {count} admin groups", userId, validGroupIds.Count);
	}
}
