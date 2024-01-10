using Jordnaer.Authorization;
using Jordnaer.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Features.DeleteUser;

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
			var deletionInitiated =
				await deleteUserService.InitiateDeleteUserAsync(currentUser.User!, cancellationToken);

			return deletionInitiated
				? TypedResults.Ok()
				: TypedResults.Unauthorized();
		});

		group.MapDelete("", async Task<Results<UnauthorizedHttpResult, Ok>> (
			HttpContext httpContext,
			[FromQuery] string token,
			[FromServices] IDeleteUserService deleteUserService,
			[FromServices] CurrentUser currentUser,
			CancellationToken cancellationToken) =>
		{
			var userDeleted = await deleteUserService.DeleteUserAsync(currentUser.User!, token, cancellationToken);
			if (!userDeleted)
			{
				return TypedResults.Unauthorized();
			}

			// TODO: Does this sign us out for external providers?
			await httpContext.SignOutAsync();

			return TypedResults.Ok();
		});

		group.MapGet("verify-token", async Task<Results<UnauthorizedHttpResult, Ok>> (
			[FromQuery] string token,
			[FromServices] IDeleteUserService deleteUserService,
			[FromServices] CurrentUser currentUser,
			CancellationToken cancellationToken) =>
		{
			var tokenIsValid = await deleteUserService.VerifyTokenAsync(currentUser.User!, token, cancellationToken);

			return tokenIsValid
				? TypedResults.Ok()
				: TypedResults.Unauthorized();
		});

		return group;
	}
}
