using Jordnaer.Server.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.GroupSearch;

public interface IGroupSearchService
{
    Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken);
}
//TODO: Register in Program.cs
public class GroupSearchService : IGroupSearchService
{
    private readonly JordnaerDbContext _context;
    private readonly ILogger<GroupSearchService> _logger;

    public GroupSearchService(JordnaerDbContext context,
        ILogger<GroupSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GroupSearchResult> GetGroupsAsync(GroupSearchFilter filter, CancellationToken cancellationToken)
    {
        // TODO: Convert to Dapper
        var groups = _context.Groups
            .AsNoTracking()
            .AsQueryable()
            .ApplyNameFilter(filter.Name)
            .ApplyCategoryFilter(filter.Categories)
            .ApplyLocationFilter(filter);

        var paginatedGroups = new List<GroupDto>();

        // TODO: Implement

        int totalCount = await groups.AsNoTracking().CountAsync(cancellationToken);

        return new GroupSearchResult { TotalCount = totalCount, Groups = paginatedGroups };
    }


}

internal static class GroupSearchServiceExtensions
{
    internal static IQueryable<Group> ApplyLocationFilter(this IQueryable<Group> groups, GroupSearchFilter filter)
    {
        // TODO
        return groups;
    }

    internal static IQueryable<Group> ApplyNameFilter(this IQueryable<Group> groups, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            groups = groups.Where(e => e.Name.Contains(name));

        return groups;
    }

    internal static IQueryable<Group> ApplyCategoryFilter(this IQueryable<Group> groups, string[]? categories)
    {
        if (categories is not null && categories.Length > 0)
            groups = groups.Where(group => group.Categories.Any(category => categories.Contains(category.Name)));

        return groups;
    }
}
