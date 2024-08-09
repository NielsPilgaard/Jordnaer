using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Membership;

public interface IMembershipService
{
	Task<OneOf<Success, Error<MembershipStatus>, Error<string>>> RequestMembership(
		string groupName,
		CancellationToken cancellationToken = default);
}

public class MembershipService(CurrentUser currentUser,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IEmailService emailService,
	ILogger<MembershipService> logger) : IMembershipService
{
	public async Task<OneOf<Success, Error<MembershipStatus>, Error<string>>> RequestMembership(
		string groupName,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var group = await context.Groups
									 .Where(x => x.Name == groupName)
									 .Include(x => x.Memberships.Where(membership => membership.UserProfileId == currentUser.Id))
									 .FirstOrDefaultAsync(cancellationToken);
			if (group is null)
			{
				return new Error<string>($"Gruppen {groupName} kunne ikke findes.");
			}

			var existingMembership = group.Memberships.FirstOrDefault(x => x.UserProfileId == currentUser.Id);
			if (existingMembership is not null)
			{
				return new Error<MembershipStatus>(existingMembership.MembershipStatus);
			}

			context.GroupMemberships.Add(new GroupMembership
			{
				GroupId = group.Id,
				UserProfileId = currentUser.Id!,
				MembershipStatus = MembershipStatus.PendingApprovalFromGroup,
				PermissionLevel = PermissionLevel.Write,
				UserInitiatedMembership = true,
				CreatedUtc = DateTime.UtcNow,
				LastUpdatedUtc = DateTime.UtcNow,
				OwnershipLevel = OwnershipLevel.Member
			});

			await context.SaveChangesAsync(cancellationToken);

			await emailService.SendMembershipRequestEmails(groupName, cancellationToken);

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}
}