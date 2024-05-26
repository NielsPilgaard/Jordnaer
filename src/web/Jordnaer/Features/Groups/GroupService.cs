using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using Serilog;
using System.Linq.Expressions;
using Jordnaer.Features.Authentication;
using NotFound = OneOf.Types.NotFound;

namespace Jordnaer.Features.Groups;

public interface IGroupService
{
	Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task<OneOf<GroupSlim, NotFound>> GetSlimGroupByNameAsync(string name, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> CreateGroupAsync(Group group, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> UpdateGroupAsync(Group group, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error, NotFound>> DeleteGroupAsync(Guid id, CancellationToken cancellationToken = default);
	Task<List<UserGroupAccess>> GetSlimGroupsForUserAsync(CancellationToken cancellationToken = default);

	Task<List<UserSlim>> GetGroupMembersByPredicateAsync(Expression<Func<GroupMembership, bool>> predicate, CancellationToken cancellationToken = default);
	Task<bool> IsCurrentUserMemberOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}

public class GroupService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILogger<GroupService> logger,
	IDiagnosticContext diagnosticContext,
	CurrentUser currentUser)
	: IGroupService
{
	public async Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups
								 .AsNoTracking()
								 .FirstOrDefaultAsync(group => group.Id == id, cancellationToken: cancellationToken);

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
				ZipCode = x.ZipCode,
				City = x.City,
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
					ZipCode = x.Group.ZipCode,
					City = x.Group.City,
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

	public async Task<bool> IsCurrentUserMemberOfGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
	{
		logger.LogFunctionBegan();

		if (currentUser.Id is null)
		{
			return false;
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var isGroupMember = await context.GroupMemberships
								   .AsNoTracking()
								   .AnyAsync(x => x.UserProfileId == currentUser.Id &&
												  x.GroupId == groupId,
											  cancellationToken);

		return isGroupMember;
	}

	public async Task<OneOf<Success, Error<string>>> CreateGroupAsync(Group group, CancellationToken cancellationToken = default)
	{
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
				PermissionLevel = PermissionLevel.Read |
								  PermissionLevel.Write |
								  PermissionLevel.Moderator |
								  PermissionLevel.Admin
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
			.AsNoTracking()
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
			return new Error<string>("Du har ikke adgang til at ændre denne gruppe.");
		}

		await UpdateExistingGroupAsync(existingGroup, group, context, cancellationToken);
		context.Entry(group).State = EntityState.Modified;
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

	private static async Task UpdateExistingGroupAsync(Group group, Group dto, JordnaerDbContext context, CancellationToken cancellationToken = default)
	{
		group.Name = dto.Name;
		group.Address = dto.Address;
		group.City = dto.City;
		group.ZipCode = dto.ZipCode;
		group.ShortDescription = dto.ShortDescription;
		group.Description = dto.Description;

		group.Categories.Clear();
		foreach (var categoryDto in dto.Categories)
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
	}
}
