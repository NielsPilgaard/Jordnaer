using Jordnaer.Shared;

namespace Jordnaer.Features.GroupSearch;

internal static class GroupSearchServiceExtensions
{
	internal static IQueryable<Group> ApplyNameFilter(this IQueryable<Group> groups, string? name)
	{
		if (!string.IsNullOrWhiteSpace(name))
		{
			groups = groups.Where(group => group.Name.Contains(name));
		}

		return groups;
	}

	internal static IQueryable<Group> ApplyCategoryFilter(this IQueryable<Group> groups, string[]? categories)
	{
		if (categories is not null && categories.Length > 0)
		{
			groups = groups.Where(group => group.Categories.Any(category => categories.Contains(category.Name)));
		}

		return groups;
	}
}
