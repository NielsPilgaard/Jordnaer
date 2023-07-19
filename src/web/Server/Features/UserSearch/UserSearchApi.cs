using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Jordnaer.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Features.UserSearch;

public static class UserSearchApi
{
    public static RouteGroupBuilder MapUserSearch(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/users/search");

        group.RequireUserSearchRateLimit();

        group.MapGet("", async Task<UserSearchResult> (
            [FromServices] IUserSearchService userService,
            [FromQuery] string? name,
            [FromQuery] string? location,
            [FromQuery] int? withinRadiusKilometers,
            [FromQuery] string[]? lookingFor,
            [FromQuery] int? minimumChildAge,
            [FromQuery] int? maximumChildAge,
            [FromQuery] string? childGender,
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken) =>
        {
            bool genderParsedSuccessfully = Enum.TryParse<Gender>(childGender, out var parsedChildGender);

            var searchFilter = new UserSearchFilter
            {
                LookingFor = lookingFor,
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

        return group;
    }
}
