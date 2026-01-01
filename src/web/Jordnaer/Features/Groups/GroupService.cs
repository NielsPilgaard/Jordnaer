using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Metrics;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using Serilog;
using System.Diagnostics;
using System.Linq.Expressions;
using NotFound = OneOf.Types.NotFound;

namespace Jordnaer.Features.Groups;

public class GroupService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<GroupService> logger,
	IDiagnosticContext diagnosticContext,
	CurrentUser currentUser,
	IGroupMembershipNotificationService notificationService)
{
	public async Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups
								 .AsNoTracking()
								 .Include(x => x.Categories)
								 .FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

		return group is null
			? new NotFound()
			: group;
	}

	public async Task<OneOf<GroupSlim, NotFound>> GetSlimGroupByNameAsync(string name, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups
			.AsNoTracking()
			.Select(x => new GroupSlim
			{
				Id = x.Id,
				Name = x.Name,
				ShortDescription = x.ShortDescription,
				Description = x.Description,
				Address = x.Address,
				ZipCode = x.ZipCode,
				City = x.City,
				Latitude = x.Location != null ? x.Location.Y : null,
				Longitude = x.Location != null ? x.Location.X : null,
				ZipCodeLatitude = x.ZipCodeLocation != null ? x.ZipCodeLocation.Y : null,
				ZipCodeLongitude = x.ZipCodeLocation != null ? x.ZipCodeLocation.X : null,
				ProfilePictureUrl = x.ProfilePictureUrl,
				MemberCount = x.Memberships.Count(membership => membership.MembershipStatus == MembershipStatus.Active),
				Categories = x.Categories.Select(category => category.Name).ToArray()
			})
			.FirstOrDefaultAsync(group => group.Name == name, cancellationToken: cancellationToken);

		return group is null
			? new NotFound()
			: group;
	}
	public async Task<List<UserGroupAccess>> GetSlimGroupsForUserAsync(CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		if (currentUser.Id is null)
		{
			return [];
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var groups = await context.GroupMemberships
			.AsNoTracking()
			.Where(membership => membership.UserProfileId == currentUser.Id &&
								 membership.MembershipStatus != MembershipStatus.Rejected)
			.Select(x => new UserGroupAccess
			{
				Group = new GroupSlim
				{
					Id = x.Group.Id,
					Name = x.Group.Name,
					ShortDescription = x.Group.ShortDescription,
					Description = x.Group.Description,
					Address = x.Group.Address,
					ZipCode = x.Group.ZipCode,
					City = x.Group.City,
					Latitude = x.Group.Location != null ? x.Group.Location.Y : null,
					Longitude = x.Group.Location != null ? x.Group.Location.X : null,
					ZipCodeLatitude = x.Group.ZipCodeLocation != null ? x.Group.ZipCodeLocation.Y : null,
					ZipCodeLongitude = x.Group.ZipCodeLocation != null ? x.Group.ZipCodeLocation.X : null,
					ProfilePictureUrl = x.Group.ProfilePictureUrl,
					MemberCount =
						x.Group.Memberships.Count(membership =>
							membership.MembershipStatus == MembershipStatus.Active),
					Categories = x.Group.Categories.Select(category => category.Name).ToArray()
				},
				MembershipStatus = x.MembershipStatus,
				OwnershipLevel = x.OwnershipLevel,
				PermissionLevel = x.PermissionLevel,
				LastUpdatedUtc = x.LastUpdatedUtc
			})
			.OrderByDescending(x => x.OwnershipLevel)
			.ThenByDescending(x => x.PermissionLevel)
			.ThenByDescending(x => x.LastUpdatedUtc)
			.ToListAsync(cancellationToken);

		return groups;
	}

	public async Task<List<UserSlim>> GetGroupMembersByPredicateAsync(Expression<Func<GroupMembership, bool>> predicate, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var members = await context.GroupMemberships
								  .AsNoTracking()
								  .Where(predicate)
								  .OrderByDescending(x => x.OwnershipLevel)
								  .ThenByDescending(x => x.PermissionLevel)
								  .Select(x => new UserSlim
								  {
									  DisplayName = x.UserProfile.DisplayName,
									  Id = x.UserProfileId,
									  ProfilePictureUrl = x.UserProfile.ProfilePictureUrl,
									  UserName = x.UserProfile.UserName
								  })
								  .ToListAsync(cancellationToken);

		return members;
	}

	public async Task<List<GroupMemberSlim>> GetGroupMembersWithRolesByPredicateAsync(Expression<Func<GroupMembership, bool>> predicate, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var members = await context.GroupMemberships
								  .AsNoTracking()
								  .Where(predicate)
								  .OrderByDescending(x => x.OwnershipLevel)
								  .ThenByDescending(x => x.PermissionLevel)
								  .Select(x => new GroupMemberSlim
								  {
									  DisplayName = x.UserProfile.DisplayName,
									  Id = x.UserProfileId,
									  ProfilePictureUrl = x.UserProfile.ProfilePictureUrl,
									  UserName = x.UserProfile.UserName,
									  OwnershipLevel = x.OwnershipLevel,
									  PermissionLevel = x.PermissionLevel
								  })
								  .ToListAsync(cancellationToken);

		return members;
	}

	public async Task<List<GroupMembershipDto>> GetGroupMembershipsAsync(string groupName, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var memberships = await context.GroupMemberships
			.AsNoTracking()
			.Where(x => x.Group.Name == groupName)
			.Select(x => new GroupMembershipDto
			{
				UserDisplayName = x.UserProfile.DisplayName,
				UserProfileId = x.UserProfileId,
				GroupId = x.GroupId,
				OwnershipLevel = x.OwnershipLevel,
				PermissionLevel = x.PermissionLevel,
				MembershipStatus = x.MembershipStatus
			})
			.ToListAsync(cancellationToken);

		return memberships;
	}

	public async Task<GroupMembershipDto?> GetCurrentUsersGroupMembershipAsync(Guid groupId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		if (currentUser.Id is null)
		{
			return null;
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var groupMembership = await context.GroupMemberships
								   .AsNoTracking()
								   .Where(x => x.UserProfileId == currentUser.Id &&
											   x.GroupId == groupId)
								   .Select(x => new GroupMembershipDto
								   {
									   UserDisplayName = x.UserProfile.DisplayName,
									   UserProfileId = x.UserProfileId,
									   GroupId = x.GroupId,
									   OwnershipLevel = x.OwnershipLevel,
									   PermissionLevel = x.PermissionLevel,
									   MembershipStatus = x.MembershipStatus
								   })
								   .SingleOrDefaultAsync(cancellationToken);

		return groupMembership;
	}

	/// <summary>
	/// Gets the count of pending membership requests for a specific group.
	/// </summary>
	public async Task<int> GetPendingMembershipCountAsync(Guid groupId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var count = await context.GroupMemberships
			.AsNoTracking()
			.CountAsync(x => x.GroupId == groupId &&
							x.MembershipStatus == MembershipStatus.PendingApprovalFromGroup,
						cancellationToken);

		return count;
	}

	/// <summary>
	/// Gets pending membership counts for all groups the current user can manage (admin or owner).
	/// Returns dictionary mapping GroupId to pending count.
	/// </summary>
	public async Task<Dictionary<Guid, int>> GetPendingMembershipCountsForUserAsync(CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		if (currentUser.Id is null)
		{
			return new Dictionary<Guid, int>();
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// Get all groups where current user is admin or owner
		var adminGroupIds = await context.GroupMemberships
			.AsNoTracking()
			.Where(x => x.UserProfileId == currentUser.Id &&
					   (x.PermissionLevel == PermissionLevel.Admin ||
						x.OwnershipLevel == OwnershipLevel.Owner))
			.Select(x => x.GroupId)
			.ToListAsync(cancellationToken);

		if (adminGroupIds.Count == 0)
		{
			return new Dictionary<Guid, int>();
		}

		// Get pending counts for those groups in a single query
		var pendingCounts = await context.GroupMemberships
			.AsNoTracking()
			.Where(x => adminGroupIds.Contains(x.GroupId) &&
					   x.MembershipStatus == MembershipStatus.PendingApprovalFromGroup)
			.GroupBy(x => x.GroupId)
			.Select(g => new { GroupId = g.Key, Count = g.Count() })
			.ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);

		return pendingCounts;
	}


	public async Task<OneOf<Success, Error<string>>> UpdateMembership(GroupMembershipDto membershipDto, CancellationToken cancellationToken = default)
	{
		Debug.Assert(currentUser.Id is not null, "Current user must be set when updating group membership.");

		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var currentUserCanManageMembers = await context.GroupMemberships
												 .AsNoTracking()
												 .AnyAsync(x => x.UserProfileId == currentUser.Id &&
																x.GroupId == membershipDto.GroupId &&
																(x.PermissionLevel == PermissionLevel.Admin ||
																 x.OwnershipLevel == OwnershipLevel.Owner),
														   cancellationToken);

		if (currentUserCanManageMembers is false)
		{
			return logger.LogAndReturnErrorResult(
				"Du har ikke rettigheder til at redigere medlemmer for denne gruppe.");
		}

		var membership = await context.GroupMemberships
									  .FirstOrDefaultAsync(
										  x => x.UserProfileId == membershipDto.UserProfileId &&
											   x.GroupId == membershipDto.GroupId, cancellationToken);
		if (membership is null)
		{
			return logger.LogAndReturnErrorResult("Medlemskabet blev ikke fundet. Kontakt venligst support hvis problemet fortsætter.");
		}

		var error = await ValidateMembershipUpdate(membership, membershipDto, context);
		if (!string.IsNullOrEmpty(error))
		{
			return logger.LogAndReturnErrorResult(error);
		}

		// Track if pending status changed to notify listeners
		var oldStatus = membership.MembershipStatus;
		var newStatus = membershipDto.MembershipStatus;
		var wasPending = oldStatus == MembershipStatus.PendingApprovalFromGroup;
		var isPending = newStatus == MembershipStatus.PendingApprovalFromGroup;

		membership.OwnershipLevel = membershipDto.OwnershipLevel;
		membership.PermissionLevel = membershipDto.PermissionLevel;
		membership.MembershipStatus = membershipDto.MembershipStatus;
		membership.LastUpdatedUtc = DateTime.UtcNow;

		context.GroupMemberships.Update(membership);

		try
		{
			await context.SaveChangesAsync(cancellationToken);
		}
		catch (Exception exception)
		{
			return logger.LogAndReturnErrorResult(exception,
				"Det lykkedes ikke at opdatere medlemskabet. Prøv igen senere.");
		}

		// Notify admins via SignalR if pending count changed
		// This is outside the DB transaction to prevent notification failures from affecting DB success
		if (wasPending != isPending)
		{
			var pendingCountChange = isPending ? 1 : -1;
			await notificationService.NotifyAdminsOfPendingCountChangeAsync(membershipDto.GroupId, pendingCountChange, cancellationToken);
		}

		return new Success();
	}

	/// <summary>
	/// It is assumed that the current user is an admin of the group,
	/// otherwise they would not be able to get this far.
	/// </summary>
	/// <param name="currentMembership"></param>
	/// <param name="updatedMembership"></param>
	/// <param name="context"></param>
	/// <returns></returns>
	public async Task<string?> ValidateMembershipUpdate(GroupMembership currentMembership,
		GroupMembershipDto updatedMembership,
		JordnaerDbContext context)
	{
		var updatingOwnMembership = currentUser.Id == currentMembership.UserProfileId;
		if (updatingOwnMembership && updatedMembership.PermissionLevel != PermissionLevel.Admin)
		{
			var adminCount = await context.GroupMemberships
										  .CountAsync(x => x.GroupId == currentMembership.GroupId &&
														  x.PermissionLevel == PermissionLevel.Admin);
			if (adminCount is 1)
			{
				return "Der skal være mindst én administrator i gruppen.";
			}
		}

		if (updatingOwnMembership && updatedMembership.OwnershipLevel != OwnershipLevel.Owner)
		{
			var ownerCount = await context.GroupMemberships
										  .CountAsync(x => x.GroupId == currentMembership.GroupId &&
														  x.OwnershipLevel == OwnershipLevel.Owner);
			if (ownerCount is 1)
			{
				return "Der skal være mindst én ejer af gruppen.";
			}
		}

		return updatedMembership.MembershipStatus is
				   not MembershipStatus.Active and
				   not MembershipStatus.Rejected
				   ? "Medlemskabsstatus kan kun sættes til aktivt eller afvist."
				   : null;
	}

	public async Task<OneOf<Success, Error<string>>> CreateGroupAsync(Group group, CancellationToken cancellationToken = default)
	{
		JordnaerMetrics.GroupsCreatedCounter.Add(1);

		logger.LogFunctionBegan();

		if (currentUser.Id is null)
		{
			return new Error<string>("Du skal være logget ind for at oprette en gruppe.");
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		if (await context.Groups.AsNoTracking().AnyAsync(x => x.Name == group.Name, cancellationToken))
		{
			return new Error<string>($"Gruppenavnet '{group.Name}' er allerede taget.");
		}

		group.Memberships =
		[
			new GroupMembership
			{
				UserProfileId = currentUser.Id,
				GroupId = group.Id,
				OwnershipLevel = OwnershipLevel.Owner,
				MembershipStatus = MembershipStatus.Active,
				PermissionLevel = PermissionLevel.Admin
			}
		];

		var selectedCategories = group.Categories.ToArray();
		group.Categories.Clear();
		foreach (var categoryDto in selectedCategories)
		{
			var category = await context.Categories.FindAsync([categoryDto.Id], cancellationToken);
			if (category is null)
			{
				group.Categories.Add(categoryDto);
				context.Entry(categoryDto).State = EntityState.Added;
			}
			else
			{
				category.LoadValuesFrom(categoryDto);
				group.Categories.Add(category);
				context.Entry(category).State = EntityState.Modified;
			}
		}

		context.Groups.Add(group);
		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("{UserId} created group '{groupName}'", currentUser.Id, group.Name);

		return new Success();
	}

	public async Task<OneOf<Success, Error<string>>> UpdateGroupAsync(Group group, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var existingGroup = await context.Groups
			.AsSingleQuery()
			.Include(e => e.Categories)
			.FirstOrDefaultAsync(e => e.Id == group.Id, cancellationToken);

		if (existingGroup is null)
		{
			return logger.LogAndReturnErrorResult("Gruppen kunne ikke findes. Prøv igen senere.");
		}

		var membership = await context.GroupMemberships
									  .FirstOrDefaultAsync(x => x.UserProfileId == currentUser.Id &&
																x.GroupId == group.Id, cancellationToken);

		if (membership?.PermissionLevel < PermissionLevel.Write)
		{
			return logger.LogAndReturnErrorResult("Du har ikke adgang til at ændre denne gruppe.");
		}

		await UpdateExistingGroupAsync(existingGroup, group, context, cancellationToken);
		context.Entry(existingGroup).State = EntityState.Modified;
		await context.SaveChangesAsync(cancellationToken);

		return new Success();
	}

	public async Task<OneOf<Success, Error, NotFound>> DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		diagnosticContext.Set("group_id", id);

		if (currentUser.Id is null)
		{
			logger.LogError("Failed to delete group because the request came from an unauthenticated user.");
			return new Error();
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups.FindAsync([id], cancellationToken);
		if (group is null)
		{
			logger.LogInformation("Failed to find group by id.");
			return new NotFound();
		}

		diagnosticContext.Set("group_name", group.Name);

		var groupOwner = await context.GroupMemberships
									  .SingleOrDefaultAsync(e => e.UserProfileId == currentUser.Id &&
																 e.OwnershipLevel == OwnershipLevel.Owner,
															 cancellationToken);

		if (groupOwner is null)
		{
			logger.LogError("Failed to delete group because it has no owner.");
			return new Error();
		}

		if (groupOwner.UserProfileId != currentUser.Id)
		{
			logger.LogError("Failed to delete group because the request came from someone other than the owner. " +
							 "The deletion was requested by the user: {@UserId}", currentUser.Id);
			return new Error();
		}

		context.Groups.Remove(group);
		await context.SaveChangesAsync(cancellationToken);

		logger.LogInformation("Successfully deleted group");

		return new Success();
	}

	private static async Task UpdateExistingGroupAsync(Group currentGroup,
		Group updatedGroup,
		JordnaerDbContext context,
		CancellationToken cancellationToken = default)
	{
		currentGroup.Name = updatedGroup.Name;
		currentGroup.Address = updatedGroup.Address;
		currentGroup.City = updatedGroup.City;
		currentGroup.ZipCode = updatedGroup.ZipCode;
		currentGroup.Location = updatedGroup.Location;
		currentGroup.ZipCodeLocation = updatedGroup.ZipCodeLocation;
		currentGroup.ShortDescription = updatedGroup.ShortDescription;
		currentGroup.Description = updatedGroup.Description;

		currentGroup.Categories.Clear();
		foreach (var categoryDto in updatedGroup.Categories)
		{
			var category = await context.Categories.FindAsync([categoryDto.Id], cancellationToken);
			if (category is null)
			{
				currentGroup.Categories.Add(categoryDto);
				context.Entry(categoryDto).State = EntityState.Added;
			}
			else
			{
				category.LoadValuesFrom(categoryDto);
				currentGroup.Categories.Add(category);
				context.Entry(category).State = EntityState.Modified;
			}
		}
	}
}

public record GroupMembershipDto
{
	public required string UserDisplayName { get; set; }
	public required string UserProfileId { get; set; }
	public required Guid GroupId { get; set; }
	public required OwnershipLevel OwnershipLevel { get; set; }
	public required PermissionLevel PermissionLevel { get; set; }
	public required MembershipStatus MembershipStatus { get; set; }
}
