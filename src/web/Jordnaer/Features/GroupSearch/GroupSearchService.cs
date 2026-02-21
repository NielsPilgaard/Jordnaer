using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Profile;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ZiggyCreatures.Caching.Fusion;

namespace Jordnaer.Features.GroupSearch;

public interface IGroupSearchService
{
	Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken = default);
}

public class GroupSearchService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	ILocationService locationService,
	IFusionCache fusionCache)
	: IGroupSearchService
{
	internal const string CacheTag = "group";
	private const string AllGroupsCacheKey = "GroupSearch:all";

	private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

	public async Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter,
		CancellationToken cancellationToken = default)
	{
		JordnaerMetrics.GroupSearchesCounter.Add(1, MakeTagList(filter));

		// Cache the unfiltered result — filtered queries always hit the DB
		if (IsUnfiltered(filter))
		{
			return await fusionCache.GetOrSetAsync<GroupSearchResult>(
				AllGroupsCacheKey,
				(_, innerToken) => FetchGroupsAsync(filter, innerToken),
				tags: [CacheTag],
				token: cancellationToken) ?? new GroupSearchResult();
		}

		return await FetchGroupsAsync(filter, cancellationToken);
	}

	private static bool IsUnfiltered(GroupSearchFilter filter) =>
		string.IsNullOrEmpty(filter.Name) &&
		(filter.Categories is null || filter.Categories.Length == 0) &&
		filter.WithinRadiusKilometers is null &&
		string.IsNullOrEmpty(filter.Location) &&
		!filter.Latitude.HasValue &&
		!filter.Longitude.HasValue;

	private async Task<GroupSearchResult> FetchGroupsAsync(GroupSearchFilter filter,
		CancellationToken cancellationToken = default)
	{
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

		var paginatedGroups = await groups
							  .Select(group => new GroupSlim
							  {
								  ProfilePictureUrl = group.ProfilePictureUrl,
								  WebsiteUrl = group.WebsiteUrl,
								  Name = group.Name,
								  ShortDescription = group.ShortDescription,
								  Description = group.Description,
								  Address = group.Address,
								  ZipCode = group.ZipCode,
								  City = group.City,
								  Latitude = group.Location != null ? group.Location.Y : null,
								  Longitude = group.Location != null ? group.Location.X : null,
								  ZipCodeLatitude = group.ZipCodeLocation != null ? group.ZipCodeLocation.Y : null,
								  ZipCodeLongitude = group.ZipCodeLocation != null ? group.ZipCodeLocation.X : null,
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
