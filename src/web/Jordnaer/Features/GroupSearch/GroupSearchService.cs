using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Jordnaer.Features.GroupSearch;

public interface IGroupSearchService
{
	Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken = default);
}

public class GroupSearchService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILocationService locationService)
	: IGroupSearchService
{
	private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);
	public async Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter,
		CancellationToken cancellationToken = default)
	{
		JordnaerMetrics.GroupSearchesCounter.Add(1, MakeTagList(filter));

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var groups = context.Groups
			.AsNoTracking()
			.AsQueryable()
			.ApplyNameFilter(filter.Name)
			.ApplyCategoryFilter(filter.Categories);

		(groups, var isOrdered) = await ApplyLocationFilterAsync(groups, filter, cancellationToken);

		if (!isOrdered)
		{
			groups = groups.OrderBy(group => group.CreatedUtc);
		}

		var totalCount = await groups.CountAsync(cancellationToken);

		var groupsToSkip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;
		var paginatedGroups = await groups
							  .Skip(groupsToSkip)
							  .Take(filter.PageSize)
							  .Select(group => new GroupSlim
							  {
								  ProfilePictureUrl = group.ProfilePictureUrl,
								  Name = group.Name,
								  ShortDescription = group.ShortDescription,
								  Description = group.Description,
								  ZipCode = group.ZipCode,
								  City = group.City,
								  Categories = group.Categories.Select(category => category.Name).ToArray(),
								  MemberCount = group.Memberships.Count(e => e.MembershipStatus == MembershipStatus.Active),
								  Id = group.Id
							  })
							  .ToListAsync(cancellationToken);

		return new GroupSearchResult { TotalCount = totalCount, Groups = paginatedGroups };
	}

	internal async Task<(IQueryable<Group> Groups, bool AppliedOrdering)> ApplyLocationFilterAsync(
		IQueryable<Group> groups,
		GroupSearchFilter filter,
		CancellationToken cancellationToken = default)
	{
		if (filter.WithinRadiusKilometers is null)
		{
			return (groups, false);
		}

		Point? location = null;

		// Prefer lat/long from map search if available
		if (filter.Latitude.HasValue && filter.Longitude.HasValue)
		{
			// NetTopologySuite Point uses (longitude, latitude) order
			location = GeometryFactory.CreatePoint(new Coordinate(filter.Longitude.Value, filter.Latitude.Value));
		}
		// Fall back to zip code/location string lookup for backward compatibility
		else if (!string.IsNullOrEmpty(filter.Location))
		{
			var searchLocation = await locationService.GetLocationFromZipCodeAsync(filter.Location, cancellationToken);
			if (searchLocation is null)
			{
				return (groups, false);
			}
			location = searchLocation.Location;
		}
		else
		{
			return (groups, false);
		}

		var radiusMeters = filter.WithinRadiusKilometers.Value * 1000;

		// Use SQL Server's built-in distance calculation with geography type
		var groupsWithDistance = groups.Where(g => g.Location != null && g.Location.IsWithinDistance(location, radiusMeters))
									   .OrderBy(g => g.Location!.Distance(location));

		return (groupsWithDistance, true);
	}

	private static ReadOnlySpan<KeyValuePair<string, object?>> MakeTagList(GroupSearchFilter filter)
	{
		return new KeyValuePair<string, object?>[]
		{
			new(nameof(filter.Location), filter.Location),
			new(nameof(filter.Categories), string.Join(',', filter.Categories ?? [])),
			new(nameof(filter.WithinRadiusKilometers), filter.WithinRadiusKilometers)
		};
	}
}
