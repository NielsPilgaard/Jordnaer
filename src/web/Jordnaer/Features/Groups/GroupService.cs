using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using Serilog;
using NotFound = OneOf.Types.NotFound;

namespace Jordnaer.Features.Groups;

public interface IGroupService
{
	Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task<OneOf<GroupSlim, NotFound>> GetSlimGroupByNameAsync(string name, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>>> CreateGroupAsync(string userId, Group group, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error<string>, NotFound>> UpdateGroupAsync(string userId, Group group, CancellationToken cancellationToken = default);
	Task<OneOf<Success, Error, NotFound>> DeleteGroupAsync(string userId, Guid id, CancellationToken cancellationToken = default);
	Task<List<UserGroupAccess>> GetSlimGroupsForUserAsync(string userId, CancellationToken cancellationToken = default);
}

public class GroupService : IGroupService
{
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory;
	private readonly ILogger<GroupService> _logger;
	private readonly IDiagnosticContext _diagnosticContext;

	public GroupService(IDbContextFactory<JordnaerDbContext> contextFactory,
		ILogger<GroupService> logger,
		IDiagnosticContext diagnosticContext)
	{
		_contextFactory = contextFactory;
		_logger = logger;
		_diagnosticContext = diagnosticContext;
	}

	public async Task<OneOf<Group, NotFound>> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)
	{
		_logger.LogFunctionBegan();

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups
								 .AsNoTracking()
								 .FirstOrDefaultAsync(group => group.Id == id, cancellationToken: cancellationToken);

		return group is null
			? new NotFound()
			: group;
	}

	public async Task<OneOf<GroupSlim, NotFound>> GetSlimGroupByNameAsync(string name, CancellationToken cancellationToken = default)
	{
		_logger.LogFunctionBegan();

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups
			.AsNoTracking()
			.Select(x => new GroupSlim
			{
				Id = x.Id,
				Name = x.Name,
				ShortDescription = x.ShortDescription,
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
	public async Task<List<UserGroupAccess>> GetSlimGroupsForUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		_logger.LogFunctionBegan();

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		var groups = await context.GroupMemberships
			.AsNoTracking()
			.Where(membership => membership.UserProfileId == userId &&
								 membership.MembershipStatus != MembershipStatus.Rejected)
			.Select(x => new UserGroupAccess
			{
				Group = new GroupSlim
				{
					Id = x.Group.Id,
					Name = x.Group.Name,
					ShortDescription = x.Group.ShortDescription,
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

	public async Task<OneOf<Success, Error<string>>> CreateGroupAsync(string userId, Group group, CancellationToken cancellationToken = default)
	{
		_logger.LogFunctionBegan();

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		if (await context.Groups.AsNoTracking().AnyAsync(x => x.Name == group.Name, cancellationToken))
		{
			return new Error<string>($"Gruppenavnet '{group.Name}' er allerede taget.");
		}

		group.Memberships = new List<GroupMembership>
		{
			new ()
			{
				UserProfileId = userId,
				GroupId = group.Id,
				OwnershipLevel = OwnershipLevel.Owner,
				MembershipStatus = MembershipStatus.Active,
				PermissionLevel = PermissionLevel.Read |
								  PermissionLevel.Write |
								  PermissionLevel.Moderator |
								  PermissionLevel.Admin
			}
		};

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

		_logger.LogInformation("{UserId} created group '{groupName}'", userId, group.Name);

		return new Success();
	}

	public async Task<OneOf<Success, Error<string>, NotFound>> UpdateGroupAsync(string userId, Group group, CancellationToken cancellationToken = default)
	{
		_logger.LogFunctionBegan();

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		if (await context.Groups.AsNoTracking().AnyAsync(x => x.Name == group.Name, cancellationToken))
		{
			return new Error<string>($"Gruppenavnet '{group.Name}' er allerede taget.");
		}

		var existingGroup = await context.Groups
			.AsNoTracking()
			.Include(e => e.Categories)
			.FirstOrDefaultAsync(e => e.Id == group.Id, cancellationToken);

		if (existingGroup is null)
		{
			return new NotFound();
		}

		var membership = await context.GroupMemberships.FirstOrDefaultAsync(x =>
																				x.UserProfileId == userId &&
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

	public async Task<OneOf<Success, Error, NotFound>> DeleteGroupAsync(string userId, Guid id, CancellationToken cancellationToken = default)
	{
		_logger.LogFunctionBegan();

		_diagnosticContext.Set("group_id", id);

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
		var group = await context.Groups.FindAsync(id);
		if (group is null)
		{
			_logger.LogInformation("Failed to find group by id.");
			return new NotFound();
		}

		_diagnosticContext.Set("group_name", group.Name);

		var groupOwner = await context.GroupMemberships
									  .SingleOrDefaultAsync(e => e.UserProfileId == userId &&
																 e.OwnershipLevel == OwnershipLevel.Owner,
															 cancellationToken);

		if (groupOwner is null)
		{
			_logger.LogError("Failed to delete group because it has no owner.");
			return new Error();
		}

		if (groupOwner.UserProfileId != userId)
		{
			_logger.LogError("Failed to delete group because the request came from someone other than the owner. " +
							 "The deletion was requested by the user: {@UserId}", userId);
			return new Error();
		}

		context.Groups.Remove(group);
		await context.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Successfully deleted group");

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
