using Jordnaer.Server.Authorization;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Jordnaer.Shared.UserSearch;
using Microsoft.AspNetCore.Mvc;

namespace Jordnaer.Server.Features.Search;

public static class UserSearchApi
{
    public static RouteGroupBuilder MapUserSearch(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/users/search");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        group.MapGet("", async Task<UserSearchResult> (
            [FromServices] IUserSearchService userService,
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] string? zipCode,
            [FromQuery] int? withinRadiusMeters,
            [FromQuery] string[]? lookingFor,
            [FromQuery] int? minimumChildAge,
            [FromQuery] int? maximumChildAge,
            [FromQuery] string? childGender,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            CancellationToken cancellationToken) =>
        {
            bool genderParsedSuccessfully = Enum.TryParse<Gender>(childGender, out var parsedChildGender);

            var searchFilter = new UserSearchFilter
            {
                LookingFor = lookingFor?.ToList() ?? new List<string>(),
                Name = name,
                Address = address,
                ChildGender = genderParsedSuccessfully ? parsedChildGender : null,
                MaximumChildAge = maximumChildAge,
                MinimumChildAge = minimumChildAge,
                PageNumber = pageNumber,
                PageSize = pageSize,
                WithinRadiusMeters = withinRadiusMeters,
                ZipCode = zipCode
            };

            var users = await userService.GetUsersAsync(searchFilter, cancellationToken);

            return users;
        });

        return group;
    }
}
