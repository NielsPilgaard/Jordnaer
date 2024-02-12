using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Profile;

public interface IProfileImageService
{
	Task<OneOf<string, Error>> SetChildProfilePictureAsync(SetChildProfilePicture dto);
	Task<OneOf<string, Error>> SetUserProfilePictureAsync(SetUserProfilePicture dto);
	Task<OneOf<string, Error>> SetGroupProfilePictureAsync(SetGroupProfilePicture dto);
}

public class ProfileImageService(JordnaerDbContext context, CurrentUser currentUser, IImageService imageService) : IProfileImageService
{
	public const string ChildProfilePicturesContainerName = "childprofile-pictures";
	public const string UserProfilePicturesContainerName = "userprofile-pictures";
	public const string GroupProfilePicturesContainerName = "groupprofile-pictures";

	public async Task<OneOf<string, Error>> SetChildProfilePictureAsync(SetChildProfilePicture dto)
	{
		if (currentUser.Id != dto.ChildProfile.UserProfileId)
		{
			return new Error();
		}

		var uri = await imageService.UploadImageAsync(dto.ChildProfile.Id.ToString("N"),
													  ChildProfilePicturesContainerName,
													  dto.FileBytes);

		await UpdateChildProfilePictureAsync(dto, uri);

		return uri;
	}

	public async Task<OneOf<string, Error>> SetUserProfilePictureAsync(SetUserProfilePicture dto)
	{
		if (currentUser.Id != dto.UserProfile.Id)
		{
			return new Error();
		}

		var uri = await imageService.UploadImageAsync(dto.UserProfile.Id,
													  UserProfilePicturesContainerName,
													  dto.FileBytes);

		await UpdateUserProfilePictureAsync(dto, uri);

		return uri;
	}

	public async Task<OneOf<string, Error>> SetGroupProfilePictureAsync(SetGroupProfilePicture dto)
	{
		if (!await CurrentUserIsGroupAdmin(dto.Group.Id))
		{
			return new Error();
		}

		var uri = await imageService.UploadImageAsync(dto.Group.Id.ToString("N"),
													  GroupProfilePicturesContainerName,
													  dto.FileBytes);

		await UpdateGroupProfilePictureAsync(dto, uri);

		return uri;
	}

	private async Task<bool> CurrentUserIsGroupAdmin(Guid groupId)
	{
		return await context.GroupMemberships
							.AsNoTracking()
							.AnyAsync(membership => membership.GroupId == groupId &&
													membership.PermissionLevel == PermissionLevel.Admin &&
													membership.UserProfileId == currentUser.Id);
	}

	private async Task UpdateChildProfilePictureAsync(SetChildProfilePicture dto, string uri)
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

	private async Task UpdateUserProfilePictureAsync(SetUserProfilePicture dto, string uri)
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
	private async Task UpdateGroupProfilePictureAsync(SetGroupProfilePicture dto, string uri)
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