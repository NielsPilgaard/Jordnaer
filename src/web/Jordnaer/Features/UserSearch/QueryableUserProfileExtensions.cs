using Jordnaer.Features.Search;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.UserSearch;

internal static class QueryableUserProfileExtensions
{
	internal static async Task<(IQueryable<UserProfile> UserProfiles, bool AppliedOrdering)> ApplyLocationFilterAsync(
		this IQueryable<UserProfile> users,
		UserSearchFilter filter,
		IZipCodeService zipCodeService,
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

	internal static IQueryable<UserProfile> ApplyCategoryFilter(this IQueryable<UserProfile> users,
		UserSearchFilter filter)
	{
		if (filter.Categories is not null && filter.Categories.Length > 0)
		{
			users = users.Where(user =>
				user.Categories.Any(category => filter.Categories.Contains(category.Name)));
		}

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

	internal static IQueryable<UserProfile> ApplyChildFilters(this IQueryable<UserProfile> users,UserSearchFilter filter)
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
