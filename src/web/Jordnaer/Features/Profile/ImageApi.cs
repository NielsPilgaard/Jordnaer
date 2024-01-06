using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Profile;

public static class ImageApi
{
	public static RouteGroupBuilder MapImages(this IEndpointRouteBuilder routes)
	{
		var group = routes.MapGroup("api/images");

		group.RequirePerUserRateLimit();

		group.MapPut("child-profile",
			async Task<Results<Ok<string>, UnauthorizedHttpResult>>
			(
				[FromBody] SetChildProfilePicture dto,
				[FromServices] JordnaerDbContext context,
				[FromServices] IImageService imageUploader,
				[FromServices] CurrentUser currentUser) =>
			{
				if (currentUser.Id != dto.ChildProfile.UserProfileId)
				{
					return TypedResults.Unauthorized();
				}

				string uri = await imageUploader.UploadImageAsync(
					dto.ChildProfile.Id.ToString("N"),
					ImageService.ChildProfilePicturesContainerName,
					dto.FileBytes);

				await SetChildProfilePictureAsync(context, dto, uri);

				return TypedResults.Ok(uri);

			}).RequireCurrentUser();


		group.MapPut("user-profile",
			async Task<Results<Ok<string>, UnauthorizedHttpResult>>
			(
				[FromBody] SetUserProfilePicture dto,
				[FromServices] JordnaerDbContext context,
				[FromServices] IImageService imageUploader,
				[FromServices] CurrentUser currentUser) =>
			{
				if (currentUser.Id != dto.UserProfile.Id)
				{
					return TypedResults.Unauthorized();
				}

				string uri = await imageUploader.UploadImageAsync(
					dto.UserProfile.Id,
					ImageService.UserProfilePicturesContainerName,
					dto.FileBytes);

				await SetUserProfilePictureAsync(context, dto, uri);

				return TypedResults.Ok(uri);

			}).RequireCurrentUser();


		group.MapPut("group",
			async Task<Results<Ok<string>, UnauthorizedHttpResult>>
			(
				[FromBody] SetGroupProfilePicture dto,
				[FromServices] JordnaerDbContext context,
				[FromServices] IImageService imageUploader,
				[FromServices] CurrentUser currentUser) =>
			{
				if (await CurrentUserIsGroupAdmin() is false)
				{
					return TypedResults.Unauthorized();
				}

				string uri = await imageUploader.UploadImageAsync(
					dto.Group.Id.ToString("N"),
					ImageService.UserProfilePicturesContainerName,
					dto.FileBytes);

				await SetGroupProfilePictureAsync(context, dto, uri);

				return TypedResults.Ok(uri);

				async Task<bool> CurrentUserIsGroupAdmin()
				{
					return await context.GroupMemberships
						.AsNoTracking()
						.AnyAsync(membership => membership.GroupId == dto.Group.Id &&
												membership.PermissionLevel == PermissionLevel.Admin &&
												membership.UserProfileId == currentUser.Id);
				}
			}).RequireCurrentUser();

		return group;
	}

	private static async Task SetChildProfilePictureAsync(JordnaerDbContext context, SetChildProfilePicture dto, string uri)
	{
		var currentChildProfile = await context.ChildProfiles.FindAsync(dto.ChildProfile.Id);
		if (currentChildProfile is null)
		{
			dto.ChildProfile.PictureUrl = uri;
			context.ChildProfiles.Add(dto.ChildProfile);
			await context.SaveChangesAsync();
			return;
		}

		// Updating is only required if the pictureUrl is not already correct
		if (currentChildProfile.PictureUrl != uri)
		{
			currentChildProfile.PictureUrl = uri;
			context.Entry(currentChildProfile).State = EntityState.Modified;

			await context.SaveChangesAsync();
		}
	}

	private static async Task SetUserProfilePictureAsync(JordnaerDbContext context, SetUserProfilePicture dto, string uri)
	{
		var currentUserProfile = await context.UserProfiles.FindAsync(dto.UserProfile.Id);
		if (currentUserProfile is null)
		{
			dto.UserProfile.ProfilePictureUrl = uri;
			context.UserProfiles.Add(dto.UserProfile);
			await context.SaveChangesAsync();
			return;
		}

		// Updating is only required if the pictureUrl is not already correct
		if (currentUserProfile.ProfilePictureUrl != uri)
		{
			currentUserProfile.ProfilePictureUrl = uri;
			context.Entry(currentUserProfile).State = EntityState.Modified;

			await context.SaveChangesAsync();
		}
	}
	private static async Task SetGroupProfilePictureAsync(JordnaerDbContext context, SetGroupProfilePicture dto, string uri)
	{
		var currentGroup = await context.Groups.FindAsync(dto.Group.Id);
		if (currentGroup is null)
		{
			dto.Group.ProfilePictureUrl = uri;
			context.Groups.Add(dto.Group);
			await context.SaveChangesAsync();
			return;
		}

		// Updating is only required if the pictureUrl is not already correct
		if (currentGroup.ProfilePictureUrl != uri)
		{
			currentGroup.ProfilePictureUrl = uri;
			context.Entry(currentGroup).State = EntityState.Modified;

			await context.SaveChangesAsync();
		}
	}
}
