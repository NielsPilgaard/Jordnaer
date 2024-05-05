using System.Diagnostics;
using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Membership;

public class MembershipService(CurrentUser currentUser,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<MembershipService> logger)
{
	public async Task<OneOf<Success, Error<string>>> RequestMembership(
		Guid groupId,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var existingMembership = await context.GroupMemberships.FirstOrDefaultAsync(x => x.GroupId == groupId &&
											 x.UserProfileId == currentUser.Id,
										 cancellationToken);

			if (existingMembership is not null)
			{
				return existingMembership.MembershipStatus switch
				{
					MembershipStatus.Active => new Error<string>("Du er allerede medlem af denne gruppe."),
					MembershipStatus.PendingApprovalFromGroup => new Error<string>(
						"Gruppen har endnu ikke svaret på din anmodning."),
					MembershipStatus.PendingApprovalFromUser => new Error<string>(
						"Gruppen har inviteret dig til at blive medlem."),
					MembershipStatus.Rejected => new Error<string>(
						"Gruppen har afvist din anmodning om at blive medlem."),
					_ => throw new UnreachableException($"Unknown {nameof(MembershipStatus)} received.")
				};
			}

			context.GroupMemberships.Add(new GroupMembership
			{
				GroupId = groupId,
				UserProfileId = currentUser.Id!,
				MembershipStatus = MembershipStatus.PendingApprovalFromGroup,
				PermissionLevel = PermissionLevel.None | PermissionLevel.Read | PermissionLevel.Write,
				UserInitiatedMembership = true,
				CreatedUtc = DateTime.UtcNow,
				LastUpdatedUtc = DateTime.UtcNow,
				OwnershipLevel = OwnershipLevel.Member
			});
			await context.SaveChangesAsync(cancellationToken);

			// TODO: Send an email to group moderators and above
			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}
}