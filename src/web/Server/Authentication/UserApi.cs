using Jordnaer.Server.Authorization;
using Jordnaer.Server.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Authentication;

public static class UserApi
{
    public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/users");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        group.MapDelete("{id}", async Task<Results<UnauthorizedHttpResult, Ok>> (
            HttpContext httpContext,
            [FromRoute] string id,
            [FromServices] IUserService userService,
            [FromServices] CurrentUser currentUser) =>
        {
            if (currentUser.Id != id)
            {
                return TypedResults.Unauthorized();
            }

            bool userDeleted = await userService.DeleteUserAsync(id);
            if (!userDeleted)
            {
                return TypedResults.Unauthorized();
            }

            await httpContext.SignOutFromAllAccountsAsync();

            return TypedResults.Ok();
        });

        return group;
    }
}
