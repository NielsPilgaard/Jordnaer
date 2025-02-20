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
		var users = context.UserProfiles.ApplyNameFilter(searchString);

		var firstTenUsers = await users
			.Where(user => user.Id != currentUserId)
			.OrderBy(user => searchString.StartsWith(searchString))
			.Take(11) // We take 11 to let the frontend know we might have more than it searched for
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

		users = users.ApplyChildFilters(filter);
		users = users.ApplyNameFilter(filter.Name);
		users = users.ApplyCategoryFilter(filter);
		(users, var isOrdered) = await users.ApplyLocationFilterAsync(filter, zipCodeService, cancellationToken);

		if (!isOrdered)
		{
			users = users.OrderBy(user => user.CreatedUtc);
		}

		// TODO: This uses a ton of memory, Dapper? (60+mb)
		// TODO: Try-catch and error in return type
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