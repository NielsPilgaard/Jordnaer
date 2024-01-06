using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Profile;

public static class ProfileApi
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
					.Include(userProfile => userProfile.Categories)
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
					.Include(userProfile => userProfile.Categories)
					.FirstOrDefaultAsync(userProfile => userProfile.Id == currentUser.Id);

				return profile is null
					? TypedResults.NotFound()
					: TypedResults.Ok(profile);
			}).RequireCurrentUser();

		group.MapPut("",
			async Task<Results<NoContent, UnauthorizedHttpResult>>
				([FromBody] UserProfile userProfile,
				[FromServices] JordnaerDbContext context,
				[FromServices] CurrentUser currentUser) =>
			{
				if (currentUser.Id != userProfile.Id)
				{
					return TypedResults.Unauthorized();
				}

				var currentUserProfile = await context.UserProfiles
					.AsSingleQuery()
					.Include(user => user.Categories)
					.Include(user => user.ChildProfiles)
					.FirstOrDefaultAsync(user => user.Id == currentUser.Id);

				if (currentUserProfile is null)
				{
					currentUserProfile = userProfile.Map();
					context.UserProfiles.Add(currentUserProfile);
				}
				else
				{
					await currentUserProfile.UpdateExistingUserProfileAsync(userProfile, context);
					context.Entry(currentUserProfile).State = EntityState.Modified;
				}

				await context.SaveChangesAsync();

				return TypedResults.NoContent();
			}).RequireCurrentUser();

		return group;
	}

	internal static async Task UpdateExistingUserProfileAsync(this UserProfile userProfile,
		UserProfile dto,
		JordnaerDbContext context)
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

		userProfile.Categories.Clear();
		foreach (var categoryDto in dto.Categories)
		{
			var category = await context.Categories.FindAsync(categoryDto.Id);
			if (category is null)
			{
				userProfile.Categories.Add(categoryDto);
				context.Entry(categoryDto).State = EntityState.Added;
			}
			else
			{
				category.LoadValuesFrom(categoryDto);
				userProfile.Categories.Add(category);
				context.Entry(category).State = EntityState.Modified;
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
