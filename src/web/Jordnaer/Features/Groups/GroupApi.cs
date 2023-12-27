using Jordnaer.Server.Authorization;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Features.Groups;

public static class GroupApi
{
    public static RouteGroupBuilder MapGroups(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/groups");

        group.RequirePerUserRateLimit();

        group.MapGet("{id:guid}", GetGroupByIdAsync).RequireCurrentUser();

        group.MapGet("slim/{name}", GetSlimGroupByNameAsync);

        group.MapGet("slim", GetSlimGroupsForUserAsync).RequireCurrentUser();

        group.MapPost("", CreateGroupAsync).RequireCurrentUser();

        group.MapPut("{id:guid}", UpdateGroupAsync).RequireCurrentUser();

        group.MapDelete("{id:guid}", DeleteGroupAsync).RequireCurrentUser();

        return group;
    }

    private static async Task<Results<Ok<Group>, NotFound>>
        GetGroupByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IGroupService groupsService)
        => await groupsService.GetGroupByIdAsync(id);

    private static async Task<Results<Ok<GroupSlim>, NotFound>>
        GetSlimGroupByNameAsync(
            [FromRoute] string name,
            [FromServices] IGroupService groupsService)
        => await groupsService.GetSlimGroupByNameAsync(name);

    private static async Task<List<UserGroupAccess>>
        GetSlimGroupsForUserAsync(
            [FromServices] IGroupService groupsService)
        => await groupsService.GetSlimGroupsForUserAsync();

    private static async Task<Results<NoContent, BadRequest<string>>> CreateGroupAsync(
        [FromBody] Group group,
        [FromServices] IGroupService groupsService)
        => await groupsService.CreateGroupAsync(group);

    private static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, BadRequest<string>>>
        UpdateGroupAsync(
            [FromRoute] Guid id,
            [FromBody] Group group,
            [FromServices] IGroupService groupsService)
        => await groupsService.UpdateGroupAsync(id, group);

    private static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>>
        DeleteGroupAsync(
            [FromRoute] Guid id,
            [FromServices] IGroupService groupsService)
        => await groupsService.DeleteGroupAsync(id);
}
