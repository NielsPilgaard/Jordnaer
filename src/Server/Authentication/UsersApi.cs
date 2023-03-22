using Jordnaer.Server.Authorization;
using Jordnaer.Server.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Authentication;

public static class UsersApi
{
    public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/users");

        group.RequireAuthorization();

        group.MapDelete("delete", async (HttpContext httpContext, [FromServices] CurrentUser currentUser, [FromServices] IUserService userService) =>
        {
            if (currentUser.User is null)
            {
                return Results.Unauthorized();
            }

            bool userDeleted = await userService.DeleteUserAsync(currentUser.User);
            if (!userDeleted)
            {
                return Results.Unauthorized();
            }

            await httpContext.SignOutAsync();
            return Results.LocalRedirect("/");
        });

        return group;
    }
}
