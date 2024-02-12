using Jordnaer.Authorization;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Features.UserSearch;

public static class UserSearchApi
{
	public static RouteGroupBuilder MapUserSearch(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("api/users/search");

		group.RequireSearchRateLimit();

		group.MapGet("", async Task<UserSearchResult> (
			[FromServices] IUserSearchService userService,
			[FromQuery] string? name,
			[FromQuery] string? location,
			[FromQuery] int? withinRadiusKilometers,
			[FromQuery] string[]? categories,
			[FromQuery] int? minimumChildAge,
			[FromQuery] int? maximumChildAge,
			[FromQuery] string? childGender,
			[FromQuery] int? pageNumber,
			[FromQuery] int? pageSize,
			CancellationToken cancellationToken) =>
		{
			var genderParsedSuccessfully = Enum.TryParse<Gender>(childGender, out var parsedChildGender);

			var searchFilter = new UserSearchFilter
			{
				Categories = categories,
				Name = name,
				Location = location,
				ChildGender = genderParsedSuccessfully ? parsedChildGender : null,
				MaximumChildAge = maximumChildAge,
				MinimumChildAge = minimumChildAge,
				PageNumber = pageNumber ?? 1,
				PageSize = pageSize ?? 10,
				WithinRadiusKilometers = withinRadiusKilometers ?? 5
			};

			var users = await userService.GetUsersAsync(searchFilter, cancellationToken);

			return users;
		});

		group.MapGet("autocomplete", async Task<Results<Ok<List<UserSlim>>, BadRequest>> (
			[FromQuery] string searchString,
			[FromServices] CurrentUser currentUser,
			[FromServices] IUserSearchService userService,
			CancellationToken cancellationToken) =>
		{
			if (string.IsNullOrWhiteSpace(searchString))
			{
				return TypedResults.BadRequest();
			}

			var users = await userService.GetUsersByNameAsync(searchString, cancellationToken);

			return TypedResults.Ok(users);
		});

		return group;
	}
}
