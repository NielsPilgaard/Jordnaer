using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Features.Groups;
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

	Task<OneOf<Success, Error<string>>> LeaveMembership(
		string groupName,
		CancellationToken cancellationToken = default);
}

public class MembershipService(CurrentUser currentUser,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IEmailService emailService,
	ILogger<MembershipService> logger,
	IGroupMembershipNotificationService notificationService) : IMembershipService
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
				// Allow re-application if user has left or been rejected
				if (existingMembership.MembershipStatus is MembershipStatus.Left or MembershipStatus.Rejected)
				{
					existingMembership.MembershipStatus = MembershipStatus.PendingApprovalFromGroup;
					existingMembership.LastUpdatedUtc = DateTime.UtcNow;
					existingMembership.UserInitiatedMembership = true;
					await context.SaveChangesAsync(cancellationToken);
					await emailService.SendMembershipRequestEmails(groupName, cancellationToken);

					// Notify admins via SignalR about the new pending request
					await notificationService.NotifyAdminsOfPendingCountChangeAsync(group.Id, 1, cancellationToken);

					return new Success();
				}

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

			// Notify admins via SignalR about the new pending request
			await notificationService.NotifyAdminsOfPendingCountChangeAsync(group.Id, 1, cancellationToken);

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}

	public async Task<OneOf<Success, Error<string>>> LeaveMembership(
		string groupName,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var membership = await context.GroupMemberships
				.Include(x => x.Group)
				.FirstOrDefaultAsync(x => x.Group.Name == groupName && x.UserProfileId == currentUser.Id, cancellationToken);

			if (membership is null)
			{
				return new Error<string>("Du er ikke medlem af denne gruppe.");
			}

			if (membership.MembershipStatus != MembershipStatus.Active)
			{
				return new Error<string>("Du kan kun forlade grupper, hvor du er et aktivt medlem.");
			}

			if (membership.OwnershipLevel == OwnershipLevel.Owner)
			{
				return new Error<string>("Ejeren kan ikke forlade gruppen. Overdrag først ejerskabet til et andet medlem.");
			}

			// Soft delete: change status to Left instead of removing the record
			membership.MembershipStatus = MembershipStatus.Left;
			membership.LastUpdatedUtc = DateTime.UtcNow;
			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}
}
