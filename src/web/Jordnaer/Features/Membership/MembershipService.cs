using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Membership;

public class MembershipService(CurrentUser currentUser,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IEmailService emailService,
	ILogger<MembershipService> logger)
{
	public async Task<OneOf<Success, Error<MembershipStatus>, Error<string>>> RequestMembership(
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
				return new Error<MembershipStatus>(existingMembership.MembershipStatus);
			}

			context.GroupMemberships.Add(new GroupMembership
			{
				GroupId = groupId,
				UserProfileId = currentUser.Id!,
				MembershipStatus = MembershipStatus.PendingApprovalFromGroup,
				PermissionLevel = PermissionLevel.Read | PermissionLevel.Write,
				UserInitiatedMembership = true,
				CreatedUtc = DateTime.UtcNow,
				LastUpdatedUtc = DateTime.UtcNow,
				OwnershipLevel = OwnershipLevel.Member
			});
			await context.SaveChangesAsync(cancellationToken);

			await emailService.SendMembershipRequestEmails(groupId, cancellationToken);

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}
}