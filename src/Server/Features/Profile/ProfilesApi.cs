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
                    .AsSingleQuery()
                    .Include(userProfile => userProfile.ChildProfiles)
                    .Include(userProfile => userProfile.LookingFor)
                    .FirstOrDefaultAsync(userProfile => userProfile.Id == id);

                return profile is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(profile);
            });

        group.MapPut("{id}",
            async Task<Results<NoContent, UnauthorizedHttpResult>>
                ([FromRoute] string id,
                [FromBody] UserProfile userProfileDto,
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
                    .FirstOrDefaultAsync(user => user.Id == id);

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
            });
        return group;
    }

    public static void LoadValuesFrom(this ChildProfile mapInto, ChildProfile mapFrom)
    {
        mapInto.CreatedUtc = mapFrom.CreatedUtc;
        mapInto.Description = mapFrom.Description;
        mapInto.DateOfBirth = mapFrom.DateOfBirth;
        mapInto.FirstName = mapFrom.FirstName;
        mapInto.LastName = mapFrom.LastName;
        mapInto.Gender = mapFrom.Gender;
        mapInto.PictureUrl = mapFrom.PictureUrl;
        mapInto.Id = mapFrom.Id;
    }
    public static void LoadValuesFrom(this LookingFor mapInto, LookingFor mapFrom)
    {
        mapInto.CreatedUtc = mapFrom.CreatedUtc;
        mapInto.Description = mapFrom.Description;
        mapInto.Name = mapFrom.Name;
        mapInto.Id = mapFrom.Id;
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
