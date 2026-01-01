using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Groups;

public interface IGroupMembershipNotificationService
{
	/// <summary>
	/// Notifies group admins and owners of a change in pending membership requests.
	/// </summary>
	/// <param name="groupId">The ID of the group</param>
	/// <param name="pendingCountChange">The change in pending count (e.g., +1 for new request, -1 for approval/rejection)</param>
	/// <param name="cancellationToken">Cancellation token</param>
	Task NotifyAdminsOfPendingCountChangeAsync(
		Guid groupId,
		int pendingCountChange,
		CancellationToken cancellationToken = default);
}

public class GroupMembershipNotificationService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IHubContext<GroupMembershipHub, IGroupMembershipHub> hubContext,
	ILogger<GroupMembershipNotificationService> logger) : IGroupMembershipNotificationService
{
	public async Task NotifyAdminsOfPendingCountChangeAsync(
		Guid groupId,
		int pendingCountChange,
		CancellationToken cancellationToken = default)
	{
		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
			var adminUserIds = await GetGroupAdminUserIdsAsync(groupId, context, cancellationToken);

			if (adminUserIds.Count > 0)
			{
				await hubContext.Clients
					.Users(adminUserIds)
					.MembershipStatusChanged(new GroupMembershipStatusChanged
					{
						GroupId = groupId,
						PendingCountChange = pendingCountChange
					});
			}
		}
		catch (Exception exception)
		{
			// Log but don't throw - notification failures shouldn't affect the operation
			logger.LogError(exception,
				"Failed to send SignalR notification for membership status change. GroupId: {GroupId}, PendingCountChange: {PendingCountChange}",
				groupId, pendingCountChange);
		}
	}

	/// <summary>
	/// Gets the user IDs of all admins and owners for a specific group.
	/// </summary>
	private static async Task<List<string>> GetGroupAdminUserIdsAsync(
		Guid groupId,
		JordnaerDbContext context,
		CancellationToken cancellationToken = default)
	{
		return await context.GroupMemberships
			.AsNoTracking()
			.Where(x => x.GroupId == groupId &&
					   x.MembershipStatus == MembershipStatus.Active &&
					   (x.PermissionLevel == PermissionLevel.Admin ||
						x.OwnershipLevel == OwnershipLevel.Owner))
			.Select(x => x.UserProfileId)
			.ToListAsync(cancellationToken);
	}
}
