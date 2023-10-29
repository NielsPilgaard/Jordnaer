using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Features.Groups;

public static class GroupSearchApi
{
    public static RouteGroupBuilder MapGroupSearch(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/groups/search");

        group.RequireSearchRateLimit();

        group.MapGet("", GetGroupsAsync);

        return group;
    }

    private static async Task<GroupSearchResult> GetGroupsAsync(
        [FromServices] IGroupSearchService groupSearchService,
        [FromQuery] string? name,
        [FromQuery] string? location,
        [FromQuery] int? withinRadiusKilometers,
        [FromQuery] string[]? categories,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var searchFilter = new GroupSearchFilter
        {
            Categories = categories,
            Name = name,
            Location = location,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 10,
            WithinRadiusKilometers = withinRadiusKilometers ?? 5
        };

        var groups = await groupSearchService.GetGroupsAsync(searchFilter, cancellationToken);

        return groups;
    }
}
