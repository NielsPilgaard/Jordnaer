using Jordnaer.Server.Authorization;
using Jordnaer.Server.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Authentication;

public static class UsersApi
{
    public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/user");

        group.RequirePerUserRateLimit();

        group.WithTags("User");

        group.MapDelete("", async Task<Results<UnauthorizedHttpResult, Ok>> (HttpContext httpContext, [FromServices] CurrentUser currentUser, [FromServices] IUserService userService) =>
        {
            if (currentUser.User is null)
            {
                return TypedResults.Unauthorized();
            }

            bool userDeleted = await userService.DeleteUserAsync(currentUser.User);
            if (!userDeleted)
            {
                return TypedResults.Unauthorized();
            }

            await httpContext.SignOutAsync();

            return TypedResults.Ok();
        });

        return group;
    }
}
