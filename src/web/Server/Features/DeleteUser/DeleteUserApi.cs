using Jordnaer.Server.Authentication;
using Jordnaer.Server.Authorization;
using Jordnaer.Server.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Features.DeleteUser;

public static class DeleteUserApi
{

    public static RouteGroupBuilder MapDeleteUsers(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/delete-user");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        group.MapGet("", async Task<Results<UnauthorizedHttpResult, Ok>> (
                    [FromServices] IDeleteUserService deleteUserService,
                    [FromServices] CurrentUser currentUser,
                    CancellationToken cancellationToken) =>
            {
                bool deletionInitiated = await deleteUserService.InitiateDeleteUserAsync(currentUser.User!, cancellationToken);

                return deletionInitiated
                    ? TypedResults.Ok()
                    : TypedResults.Unauthorized();
            })
            .RequireCurrentUser();

        group.MapDelete("", async Task<Results<UnauthorizedHttpResult, Ok>> (
            HttpContext httpContext,
            [FromQuery] string token,
            [FromServices] IDeleteUserService deleteUserService,
            [FromServices] CurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            bool userDeleted = await deleteUserService.DeleteUserAsync(currentUser.User!, token, cancellationToken);
            if (!userDeleted)
            {
                return TypedResults.Unauthorized();
            }

            await httpContext.SignOutFromAllAccountsAsync();

            return TypedResults.Ok();

        }).RequireCurrentUser();
        return group;
    }
}
