using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Search;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.UserSearch;

public interface IUserSearchService
{
	Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken = default);
	Task<List<UserSlim>> GetUsersByNameAsync(string currentUserId, string searchString, CancellationToken cancellationToken = default);
}

public class UserSearchService(
	IZipCodeService zipCodeService,
	IDbContextFactory<JordnaerDbContext> contextFactory)
	: IUserSearchService
{

	public async Task<List<UserSlim>> GetUsersByNameAsync(string currentUserId, string searchString, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var users = ApplyNameFilter(searchString, context.UserProfiles);

		var firstTenUsers = await users
			.Where(user => user.Id != currentUserId)
			.OrderBy(user => searchString.StartsWith(searchString))
			.Take(11)
			.Select(user => new UserSlim
			{
				ProfilePictureUrl = user.ProfilePictureUrl,
				DisplayName = $"{user.FirstName} {user.LastName}",
				Id = user.Id,
				UserName = user.UserName
			})
		   .AsNoTracking()
		   .ToListAsync(cancellationToken);

		return firstTenUsers;
	}

	public async Task<UserSearchResult> GetUsersAsync(UserSearchFilter filter, CancellationToken cancellationToken = default)
	{
		JordnaerMetrics.UserSearchesCounter.Add(1, MakeTagList(filter));

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var users = context.UserProfiles
							.Where(user => !string.IsNullOrEmpty(user.UserName))
							.AsQueryable();

		users = ApplyChildFilters(filter, users);
		users = ApplyNameFilter(filter.Name, users);
		users = ApplyCategoryFilter(filter, users);
		(users, var isOrdered) = await ApplyLocationFilterAsync(filter, users, cancellationToken);

		if (!isOrdered)
		{
			users = users.OrderBy(user => user.CreatedUtc);
		}

		// TODO: This uses a ton of memory, Dapper? (60+mb)
		var usersToSkip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;
		var paginatedUsers = await users
			.Skip(usersToSkip)
			.Take(filter.PageSize)
			.Include(user => user.Categories)
			.Include(user => user.ChildProfiles)
			.AsSplitQuery()
			.Select(user => new UserDto
			{
				ProfilePictureUrl = user.ProfilePictureUrl,
				UserName = user.UserName,
				FirstName = user.FirstName,
				LastName = user.LastName,
				ZipCode = user.ZipCode,
				City = user.City,
				Categories = user.Categories.Select(category => category.Name).ToList(),
				Children = user.ChildProfiles.Select(child => new ChildDto
				{
					FirstName = child.FirstName,
					LastName = child.LastName,
					Gender = child.Gender,
					DateOfBirth = child.DateOfBirth,
					Age = child.Age
				}).ToList()
			})
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		var totalCount = await users.AsNoTracking().CountAsync(cancellationToken);

		return new UserSearchResult { TotalCount = totalCount, Users = paginatedUsers };
	}

	private async Task<(IQueryable<UserProfile> UserProfiles, bool AppliedOrdering)> ApplyLocationFilterAsync(
		UserSearchFilter filter,
		IQueryable<UserProfile> users,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
		{
			return (users, false);
		}

		var (zipCodesWithinCircle, searchedZipCode) = await zipCodeService.GetZipCodesNearLocationAsync(
										 filter.Location,
										 filter.WithinRadiusKilometers.Value,
										 cancellationToken);

		if (zipCodesWithinCircle.Count is 0 || searchedZipCode is null)
		{
			return (users, false);
		}

		users = users.Where(user => user.ZipCode != null &&
									zipCodesWithinCircle.Contains(user.ZipCode.Value))
					 .OrderBy(user => Math.Abs(user.ZipCode!.Value - searchedZipCode.Value));

		return (users, true);
	}

	private static IQueryable<UserProfile> ApplyCategoryFilter(UserSearchFilter filter, IQueryable<UserProfile> users)
	{
		if (filter.Categories is not null && filter.Categories.Length > 0)
		{
			users = users.Where(user =>
				user.Categories.Any(category => filter.Categories.Contains(category.Name)));
		}

		return users;
	}

	private static IQueryable<UserProfile> ApplyNameFilter(string? filter, IQueryable<UserProfile> users)
	{
		if (!string.IsNullOrWhiteSpace(filter))
		{
			users = users.Where(user => !string.IsNullOrEmpty(user.SearchableName) &&
										EF.Functions.Like(user.SearchableName, $"%{filter}%"));
		}

		return users;
	}

	private static IQueryable<UserProfile> ApplyChildFilters(UserSearchFilter filter, IQueryable<UserProfile> users)
	{
		if (filter.ChildGender is not null)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Gender == filter.ChildGender));
		}

		if (filter is { MinimumChildAge: not null, MaximumChildAge: not null } &&
			filter.MinimumChildAge == filter.MaximumChildAge)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Age != null &&
												child.Age == filter.MinimumChildAge));
			return users;
		}

		if (filter.MinimumChildAge is not null)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Age != null &&
												child.Age >= filter.MinimumChildAge));
		}

		if (filter.MaximumChildAge is not null)
		{
			users = users.Where(user =>
				user.ChildProfiles.Any(child => child.Age != null &&
												child.Age <= filter.MaximumChildAge));
		}

		return users;
	}

	private static ReadOnlySpan<KeyValuePair<string, object?>> MakeTagList(UserSearchFilter filter)
	{
		return new KeyValuePair<string, object?>[]
		{
			new(nameof(filter.Location), filter.Location),
			new(nameof(filter.Categories), string.Join(',', filter.Categories ?? [])),
			new(nameof(filter.WithinRadiusKilometers), filter.WithinRadiusKilometers)
		};
	}
}