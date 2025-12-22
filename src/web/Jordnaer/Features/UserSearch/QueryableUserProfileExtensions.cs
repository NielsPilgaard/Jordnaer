using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.UserSearch;

internal static class QueryableUserProfileExtensions
{
	internal static async Task<(List<UserProfile> UserProfiles, bool AppliedOrdering)> ApplyLocationFilter(
		this IQueryable<UserProfile> users,
		UserSearchFilter filter,
		ILocationService locationService,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
		{
			return (await users.ToListAsync(cancellationToken), false);
		}

		// Get location from the search location
		// TODO: When we get a map in our user, group and post search filters, we should use that location instead and remove async 
		var searchLocation = await locationService.GetLocationFromZipCodeAsync(filter.Location, cancellationToken);

		if (!searchLocation.HasValue)
		{
			return (await users.ToListAsync(cancellationToken), false);
		}

		var location = searchLocation.Value.Location;
		var radiusMeters = filter.WithinRadiusKilometers.Value * 1000;

		// Use SQL Server's built-in distance calculation with geography type
		var usersWithDistance = await users
			.Where(u => u.Location != null && u.Location.Distance(location) <= radiusMeters)
			.OrderBy(u => u.Location!.Distance(location))
			.ToListAsync(cancellationToken);

		return (usersWithDistance, true);
	}

	internal static IQueryable<UserProfile> ApplyCategoryFilter(this IQueryable<UserProfile> users,
		UserSearchFilter filter)
	{
		if (filter.Categories is null || filter.Categories.Length is 0)
		{
			return users;
		}

		// This ToList prevents a LINQ translation issue on Ubuntu
		var categories = filter.Categories.ToList();

		users = users.Where(user =>
								user.Categories.Any(category => categories.Contains(category.Name)));

		return users;
	}

	internal static IQueryable<UserProfile> ApplyNameFilter(this IQueryable<UserProfile> users, string? filter)
	{
		if (!string.IsNullOrWhiteSpace(filter))
		{
			users = users.Where(user => !string.IsNullOrEmpty(user.SearchableName) &&
										EF.Functions.Like(user.SearchableName, $"%{filter}%"));
		}

		return users;
	}

	internal static IQueryable<UserProfile> ApplyChildFilters(this IQueryable<UserProfile> users, UserSearchFilter filter)
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
}
