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
                    //.Include(userProfile => userProfile.ChildProfiles)
                    //.Include(userProfile => userProfile.LookingFor)
                    .FirstOrDefaultAsync(userProfile => userProfile.Id == id);

                return profile is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(profile);
            });
        group.MapPut("{id}",
            async Task<Results<NoContent, UnauthorizedHttpResult>>
                ([FromRoute] string id,
                [FromBody] UserProfileDto userProfileDto,
                [FromServices] JordnaerDbContext context,
                [FromServices] CurrentUser currentUser) =>
            {
                if (currentUser.Id != userProfileDto.Id)
                {
                    return TypedResults.Unauthorized();
                }

                var userProfile = userProfileDto.Map();

                context.UserProfiles.Update(userProfile);

                await context.SaveChangesAsync();

                return TypedResults.NoContent();
            });
        return group;
    }
}
