using Jordnaer.Database;
using Jordnaer.Features.Metrics;
using Jordnaer.Features.Search;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.GroupSearch;

public interface IGroupSearchService
{
	Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken = default);
}

public class GroupSearchService(
	JordnaerDbContext context,
	IZipCodeService zipCodeService)
	: IGroupSearchService
{
	public async Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter,
		CancellationToken cancellationToken = default)
	{
		JordnaerMetrics.GroupSearchesCounter.Add(1, MakeTagList(filter));

		var groups = context.Groups
			.AsNoTracking()
			.AsQueryable()
			.ApplyNameFilter(filter.Name)
			.ApplyCategoryFilter(filter.Categories);

		(groups, var isOrdered) = await ApplyLocationFilterAsync(groups, filter, cancellationToken);

		if (!isOrdered)
		{
			groups = groups.OrderBy(user => user.CreatedUtc);
		}

		// TODO: Try-catch and error in return type
		var groupsToSkip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;
		var paginatedGroups = await groups
			.Skip(groupsToSkip)
			.Take(filter.PageSize)
			.Include(user => user.Categories)
			.AsSingleQuery()
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
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		var totalCount = await groups.AsNoTracking().CountAsync(cancellationToken);

		return new GroupSearchResult { TotalCount = totalCount, Groups = paginatedGroups };
	}

	internal async Task<(IQueryable<Group> Groups, bool AppliedOrdering)>
		ApplyLocationFilterAsync(IQueryable<Group> groups, GroupSearchFilter filter, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(filter.Location) || filter.WithinRadiusKilometers is null)
		{
			return (groups, false);
		}

		var (zipCodesWithinCircle, searchedZipCode) = await zipCodeService.GetZipCodesNearLocationAsync(
														  filter.Location,
														  filter.WithinRadiusKilometers.Value,
														  cancellationToken);

		if (zipCodesWithinCircle.Count is 0 || searchedZipCode is null)
		{
			return (groups, false);
		}

		groups = groups
			.Where(group => group.ZipCode != null &&
							zipCodesWithinCircle.Contains(group.ZipCode.Value))
			.OrderBy(group => Math.Abs(group.ZipCode!.Value - searchedZipCode.Value));

		return (groups, true);
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
