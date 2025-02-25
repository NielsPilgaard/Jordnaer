using Jordnaer.Shared;

namespace Jordnaer.Features.GroupSearch;

internal static class QueryableGroupExtensions
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
		if (categories is null || categories.Length is 0)
		{
			return groups;
		}

		// This ToList prevents a LINQ translation issue on Ubuntu
		var categoriesList = categories.ToList();

		groups = groups.Where(group => group.Categories.Any(category => categoriesList.Contains(category.Name)));

		return groups;
	}
}
