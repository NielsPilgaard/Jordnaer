using System.Security.Cryptography;
using System.Text;
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

	Task<OneOf<Success, Error<string>>> InviteUserToGroupAsync(
		Guid groupId,
		string userId,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> InviteByEmailAsync(
		Guid groupId,
		string email,
		CancellationToken cancellationToken = default);

	Task<OneOf<PendingGroupInvite, NotFound, Error<string>>> ValidateInviteTokenAsync(
		string token,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, NotFound, Error<string>>> AcceptPendingInviteAsync(
		string userId,
		string token,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> AcceptGroupInviteAsync(
		Guid groupId,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, Error<string>>> DeclineGroupInviteAsync(
		Guid groupId,
		CancellationToken cancellationToken = default);

	Task<List<GroupInviteDto>> GetPendingInvitesForUserAsync(
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

					// Send email notification - don't fail the request if notification fails
					try
					{
						await emailService.SendMembershipRequestEmails(groupName, cancellationToken);
					}
					catch (Exception notificationException)
					{
						logger.LogError(notificationException, "Failed to send membership request emails for group {GroupName}", groupName);
					}

					// Notify admins via SignalR - don't fail the request if notification fails
					try
					{
						await notificationService.NotifyAdminsOfPendingCountChangeAsync(group.Id, 1, cancellationToken);
					}
					catch (Exception notificationException)
					{
						logger.LogError(notificationException, "Failed to send SignalR notification for group {GroupId}", group.Id);
					}

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

			// Send email notification - don't fail the request if notification fails
			try
			{
				await emailService.SendMembershipRequestEmails(groupName, cancellationToken);
			}
			catch (Exception notificationException)
			{
				logger.LogError(notificationException, "Failed to send membership request emails for group {GroupName}", groupName);
			}

			// Notify admins via SignalR - don't fail the request if notification fails
			try
			{
				await notificationService.NotifyAdminsOfPendingCountChangeAsync(group.Id, 1, cancellationToken);
			}
			catch (Exception notificationException)
			{
				logger.LogError(notificationException, "Failed to send SignalR notification for group {GroupId}", group.Id);
			}

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

	public async Task<OneOf<Success, Error<string>>> InviteUserToGroupAsync(
		Guid groupId,
		string userId,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			// Prevent self-invitation
			if (userId == currentUser.Id)
			{
				return new Error<string>("Du kan ikke invitere dig selv til en gruppe.");
			}

			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			// Check if current user is admin or owner of the group
			var currentUserCanInvite = await context.GroupMemberships
				.AsNoTracking()
				.AnyAsync(x => x.GroupId == groupId &&
							   x.UserProfileId == currentUser.Id &&
							   x.MembershipStatus == MembershipStatus.Active &&
							   (x.PermissionLevel == PermissionLevel.Admin ||
								x.OwnershipLevel == OwnershipLevel.Owner),
					cancellationToken);

			if (!currentUserCanInvite)
			{
				return new Error<string>("Du har ikke tilladelse til at invitere medlemmer til denne gruppe.");
			}

			// Get group first
			var group = await context.Groups.FindAsync([groupId], cancellationToken);
			if (group is null)
			{
				return new Error<string>("Gruppen kunne ikke findes.");
			}

			// Check if user already has a membership (active, pending, etc.)
			var existingMembership = await context.GroupMemberships
				.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserProfileId == userId, cancellationToken);

			if (existingMembership is not null)
			{
				// Allow re-invitation if user has left or been rejected
				if (existingMembership.MembershipStatus is MembershipStatus.Left or MembershipStatus.Rejected)
				{
					existingMembership.MembershipStatus = MembershipStatus.PendingApprovalFromUser;
					existingMembership.LastUpdatedUtc = DateTime.UtcNow;
					existingMembership.UserInitiatedMembership = false;
					await context.SaveChangesAsync(cancellationToken);

					// Send email notification
					try
					{
						await emailService.SendGroupInviteEmail(group.Name, userId, cancellationToken);
					}
					catch (Exception notificationException)
					{
						logger.LogError(notificationException, "Failed to send invite email for group {GroupName}", group.Name);
					}

					return new Success();
				}

				return new Error<string>("Brugeren er allerede medlem af gruppen eller har en aktiv anmodning.");
			}

			// Create new membership with PendingApprovalFromUser status
			context.GroupMemberships.Add(new GroupMembership
			{
				GroupId = groupId,
				UserProfileId = userId,
				MembershipStatus = MembershipStatus.PendingApprovalFromUser,
				PermissionLevel = PermissionLevel.Write,
				UserInitiatedMembership = false,
				CreatedUtc = DateTime.UtcNow,
				LastUpdatedUtc = DateTime.UtcNow,
				OwnershipLevel = OwnershipLevel.Member
			});

			await context.SaveChangesAsync(cancellationToken);

			// Send email notification - don't fail the request if notification fails
			try
			{
				await emailService.SendGroupInviteEmail(group.Name, userId, cancellationToken);
			}
			catch (Exception notificationException)
			{
				logger.LogError(notificationException, "Failed to send invite email for group {GroupName}", group.Name);
			}

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}

	public async Task<OneOf<Success, Error<string>>> AcceptGroupInviteAsync(
		Guid groupId,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var membership = await context.GroupMemberships
				.FirstOrDefaultAsync(x => x.GroupId == groupId &&
										  x.UserProfileId == currentUser.Id &&
										  x.MembershipStatus == MembershipStatus.PendingApprovalFromUser,
					cancellationToken);

			if (membership is null)
			{
				return new Error<string>("Invitationen kunne ikke findes.");
			}

			membership.MembershipStatus = MembershipStatus.Active;
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

	public async Task<OneOf<Success, Error<string>>> DeclineGroupInviteAsync(
		Guid groupId,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var membership = await context.GroupMemberships
				.FirstOrDefaultAsync(x => x.GroupId == groupId &&
										  x.UserProfileId == currentUser.Id &&
										  x.MembershipStatus == MembershipStatus.PendingApprovalFromUser,
					cancellationToken);

			if (membership is null)
			{
				return new Error<string>("Invitationen kunne ikke findes.");
			}

			// Remove the membership record when declined
			context.GroupMemberships.Remove(membership);
			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}

	public async Task<List<GroupInviteDto>> GetPendingInvitesForUserAsync(
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var invites = await context.GroupMemberships
				.AsNoTracking()
				.Where(x => x.UserProfileId == currentUser.Id &&
							x.MembershipStatus == MembershipStatus.PendingApprovalFromUser)
				.Select(x => new GroupInviteDto
				{
					GroupId = x.GroupId,
					GroupName = x.Group.Name,
					GroupDescription = x.Group.Description,
					GroupProfilePictureUrl = x.Group.ProfilePictureUrl,
					InvitedByUserName = "Gruppe administrator", // We don't track who sent the invite currently
					InvitedAtUtc = x.CreatedUtc
				})
				.ToListAsync(cancellationToken);

			return invites;
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return [];
		}
	}

	public async Task<OneOf<Success, Error<string>>> InviteByEmailAsync(
		Guid groupId,
		string email,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			// Check if current user is admin or owner of the group
			var currentUserCanInvite = await context.GroupMemberships
				.AsNoTracking()
				.AnyAsync(x => x.GroupId == groupId &&
							   x.UserProfileId == currentUser.Id &&
							   x.MembershipStatus == MembershipStatus.Active &&
							   (x.PermissionLevel == PermissionLevel.Admin ||
								x.OwnershipLevel == OwnershipLevel.Owner),
					cancellationToken);

			if (!currentUserCanInvite)
			{
				return new Error<string>("Du har ikke tilladelse til at invitere medlemmer til denne gruppe.");
			}

			// Get group
			var group = await context.Groups.FindAsync([groupId], cancellationToken);
			if (group is null)
			{
				return new Error<string>("Gruppen kunne ikke findes.");
			}

			// Check if email is already registered
			var existingUser = await context.Users
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

			if (existingUser is not null)
			{
				// User exists - use existing invite flow
				return await InviteUserToGroupAsync(groupId, existingUser.Id, cancellationToken);
			}

			// Check if there's already a pending invite for this email/group
			var existingInvite = await context.PendingGroupInvites
				.FirstOrDefaultAsync(x => x.Email == email &&
										  x.GroupId == groupId &&
										  x.Status == PendingInviteStatus.Pending,
					cancellationToken);

			if (existingInvite is not null)
			{
				// Check if it's expired
				if (existingInvite.ExpiresAtUtc < DateTime.UtcNow)
				{
					// Expire the old invite and create a new one
					existingInvite.Status = PendingInviteStatus.Expired;
				}
				else
				{
					return new Error<string>("Der er allerede sendt en invitation til denne email-adresse.");
				}
			}

			// Generate secure token
			var token = GenerateSecureToken();
			var tokenHash = HashToken(token);

			// Create pending invite
			var pendingInvite = new PendingGroupInvite
			{
				Id = Guid.NewGuid(),
				GroupId = groupId,
				Email = email,
				TokenHash = tokenHash,
				Status = PendingInviteStatus.Pending,
				CreatedUtc = DateTime.UtcNow,
				ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
				InvitedByUserId = currentUser.Id
			};

			context.PendingGroupInvites.Add(pendingInvite);
			await context.SaveChangesAsync(cancellationToken);

			// Send email with invite link
			try
			{
				await emailService.SendGroupInviteEmailToNewUserAsync(email, group.Name, token, cancellationToken);
			}
			catch (Exception notificationException)
			{
				logger.LogError(notificationException, "Failed to send invite email to {Email} for group {GroupName}", MaskedEmail.Mask(email), group.Name);
			}

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}

	public async Task<OneOf<PendingGroupInvite, NotFound, Error<string>>> ValidateInviteTokenAsync(
		string token,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var tokenHash = HashToken(token);

			var invite = await context.PendingGroupInvites
				.Include(x => x.Group)
				.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

			if (invite is null)
			{
				return new NotFound();
			}

			if (invite.Status == PendingInviteStatus.Accepted)
			{
				return new Error<string>("Denne invitation er allerede blevet brugt.");
			}

			if (invite.Status == PendingInviteStatus.Expired || invite.ExpiresAtUtc < DateTime.UtcNow)
			{
				// Mark as expired if not already
				if (invite.Status != PendingInviteStatus.Expired)
				{
					invite.Status = PendingInviteStatus.Expired;
					await context.SaveChangesAsync(cancellationToken);
				}

				return new Error<string>("Denne invitation er udløbet. Bed om en ny invitation.");
			}

			return invite;
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}

	public async Task<OneOf<Success, NotFound, Error<string>>> AcceptPendingInviteAsync(
		string userId,
		string token,
		CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			var tokenHash = HashToken(token);

			var invite = await context.PendingGroupInvites
				.Include(x => x.Group)
				.FirstOrDefaultAsync(x => x.TokenHash == tokenHash &&
										  x.Status == PendingInviteStatus.Pending,
					cancellationToken);

			if (invite is null)
			{
				return new NotFound();
			}

			if (invite.ExpiresAtUtc < DateTime.UtcNow)
			{
				invite.Status = PendingInviteStatus.Expired;
				await context.SaveChangesAsync(cancellationToken);
				return new Error<string>("Denne invitation er udløbet.");
			}

			// Check if user already has a membership
			var existingMembership = await context.GroupMemberships
				.FirstOrDefaultAsync(x => x.GroupId == invite.GroupId && x.UserProfileId == userId, cancellationToken);

			if (existingMembership is not null)
			{
				if (existingMembership.MembershipStatus == MembershipStatus.Active)
				{
					// Already a member - just mark invite as accepted
					invite.Status = PendingInviteStatus.Accepted;
					invite.AcceptedAtUtc = DateTime.UtcNow;
					await context.SaveChangesAsync(cancellationToken);
					return new Success();
				}

				// Update existing membership to active
				existingMembership.MembershipStatus = MembershipStatus.Active;
				existingMembership.LastUpdatedUtc = DateTime.UtcNow;
				existingMembership.UserInitiatedMembership = false;
			}
			else
			{
				// Create new membership
				context.GroupMemberships.Add(new GroupMembership
				{
					GroupId = invite.GroupId,
					UserProfileId = userId,
					MembershipStatus = MembershipStatus.Active,
					PermissionLevel = PermissionLevel.Write,
					UserInitiatedMembership = false,
					CreatedUtc = DateTime.UtcNow,
					LastUpdatedUtc = DateTime.UtcNow,
					OwnershipLevel = OwnershipLevel.Member
				});
			}

			// Mark invite as accepted
			invite.Status = PendingInviteStatus.Accepted;
			invite.AcceptedAtUtc = DateTime.UtcNow;

			await context.SaveChangesAsync(cancellationToken);

			return new Success();
		}
		catch (Exception exception)
		{
			logger.LogException(exception);
			return new Error<string>("Der skete en fejl. Prøv igen senere.");
		}
	}

	private static string GenerateSecureToken()
	{
		var bytes = new byte[32];
		RandomNumberGenerator.Fill(bytes);
		return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
	}

	private static string HashToken(string token)
	{
		var bytes = Encoding.UTF8.GetBytes(token);
		var hash = SHA256.HashData(bytes);
		return Convert.ToBase64String(hash);
	}
}
