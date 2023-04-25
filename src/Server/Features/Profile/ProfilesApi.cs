using Jordnaer.Server.Authorization;
using Jordnaer.Server.Database;
using Jordnaer.Server.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Server.Features.Profile;

public static class ProfilesApi
{
    public static RouteGroupBuilder MapProfiles(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/profiles");

        group.RequireAuthorization(builder => builder.RequireCurrentUser());

        group.RequirePerUserRateLimit();

        group.MapGet("{id}",
            async Task<Results<Ok<UserProfile>, NotFound>>
                ([FromRoute] string id, [FromServices] JordnaerDbContext context) =>
            {
                var profile = await context
                    .UserProfiles
                    .AsNoTracking()
                    .Include(userProfile => userProfile.ChildProfiles)
                    .Include(userProfile => userProfile.LookingFor)
                    .FirstOrDefaultAsync(userProfile => userProfile.ApplicationUserId == id);

                return profile is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(profile);
            });
        group.MapPut("{id}",
            async Task<Results<NoContent, NotFound>>
                ([FromRoute] string id, [FromBody] UserProfileDto userProfileDto, [FromServices] JordnaerDbContext context) =>
            {
                int updatedRows = await context.UserProfiles.ExecuteUpdateAsync(calls =>
                    calls.SetProperty(userProfile => userProfile, userProfileDto.Map()));

                return updatedRows > 0
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            });
        return group;
    }
}