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

        group.RequirePerUserRateLimit();

        group.MapGet("{userName}",
            async Task<Results<Ok<ProfileDto>, NotFound>>
                ([FromRoute] string userName, [FromServices] JordnaerDbContext context) =>
            {
                var profile = await context
                    .UserProfiles
                    .AsNoTracking()
                    .AsSingleQuery()
                    .Include(userProfile => userProfile.ChildProfiles)
                    .Include(userProfile => userProfile.LookingFor)
                    .Where(userProfile => userProfile.UserName == userName)
                    .Select(userProfile => userProfile.ToProfileDto())
                    .FirstOrDefaultAsync();

                return profile is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(profile);
            });

        group.MapGet("",
            async Task<Results<Ok<UserProfile>, NotFound>>
                ([FromServices] CurrentUser currentUser, [FromServices] JordnaerDbContext context) =>
            {
                var profile = await context
                    .UserProfiles
                    .AsNoTracking()
                    .AsSingleQuery()
                    .Include(userProfile => userProfile.ChildProfiles)
                    .Include(userProfile => userProfile.LookingFor)
                    .FirstOrDefaultAsync(userProfile => userProfile.Id == currentUser.Id);

                return profile is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(profile);
            }).RequireCurrentUser();

        group.MapPut("",
            async Task<Results<NoContent, UnauthorizedHttpResult>>
                ([FromBody] UserProfile userProfileDto,
                [FromServices] JordnaerDbContext context,
                [FromServices] CurrentUser currentUser) =>
            {
                if (currentUser.Id != userProfileDto.Id)
                {
                    return TypedResults.Unauthorized();
                }

                var userProfile = await context.UserProfiles
                    .AsSingleQuery()
                    .Include(user => user.LookingFor)
                    .Include(user => user.ChildProfiles)
                    .FirstOrDefaultAsync(user => user.Id == currentUser.Id);

                if (userProfile is null)
                {
                    userProfile = userProfileDto.Map();
                    context.UserProfiles.Add(userProfile);
                }
                else
                {
                    await userProfile.LoadValuesFromAsync(userProfileDto, context);
                    context.Entry(userProfile).State = EntityState.Modified;
                }

                await context.SaveChangesAsync();

                return TypedResults.NoContent();
            }).RequireCurrentUser();
        return group;
    }

    public static async Task LoadValuesFromAsync(this UserProfile userProfile, UserProfile dto, JordnaerDbContext context)
    {
        userProfile.FirstName = dto.FirstName;
        userProfile.LastName = dto.LastName;
        userProfile.Address = dto.Address;
        userProfile.City = dto.City;
        userProfile.ZipCode = dto.ZipCode;
        userProfile.DateOfBirth = dto.DateOfBirth;
        userProfile.Description = dto.Description;
        userProfile.PhoneNumber = dto.PhoneNumber;
        userProfile.ProfilePictureUrl = dto.ProfilePictureUrl;
        userProfile.UserName = dto.UserName;

        userProfile.LookingFor.Clear();
        foreach (var lookingForDto in dto.LookingFor)
        {
            var lookingFor = await context.LookingFor.FindAsync(lookingForDto.Id);
            if (lookingFor is null)
            {
                userProfile.LookingFor.Add(lookingForDto);
                context.Entry(lookingForDto).State = EntityState.Added;
            }
            else
            {
                lookingFor.LoadValuesFrom(lookingForDto);
                userProfile.LookingFor.Add(lookingFor);
                context.Entry(lookingFor).State = EntityState.Modified;
            }
        }

        userProfile.ChildProfiles.Clear();
        foreach (var childProfileDto in dto.ChildProfiles)
        {
            var childProfile = await context.ChildProfiles.FindAsync(childProfileDto.Id);
            if (childProfile is null)
            {
                userProfile.ChildProfiles.Add(childProfileDto);
                context.Entry(childProfileDto).State = EntityState.Added;
            }
            else
            {
                childProfile.LoadValuesFrom(childProfileDto);
                userProfile.ChildProfiles.Add(childProfile);
                context.Entry(childProfile).State = EntityState.Modified;
            }
        }
    }
}
