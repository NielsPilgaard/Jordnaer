using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Profile;

public interface IProfileImageService
{
	Task<string> SetChildProfilePictureAsync(SetChildProfilePicture dto);
	Task<string> SetUserProfilePictureAsync(SetUserProfilePicture dto);
	Task<string> SetGroupProfilePictureAsync(SetGroupProfilePicture dto);
}

public class ProfileImageService(JordnaerDbContext context, IImageService imageService) : IProfileImageService
{
	public const string ChildProfilePicturesContainerName = "childprofile-pictures";
	public const string UserProfilePicturesContainerName = "userprofile-pictures";
	public const string GroupProfilePicturesContainerName = "groupprofile-pictures";

	public async Task<string> SetChildProfilePictureAsync(SetChildProfilePicture dto)
	{
		var uri = await imageService.UploadImageAsync(dto.ChildProfile.Id.ToString("N"),
													  ChildProfilePicturesContainerName,
													  dto.FileBytes);

		await UpdateChildProfilePictureAsync(dto, uri);

		return uri;
	}

	public async Task<string> SetUserProfilePictureAsync(SetUserProfilePicture dto)
	{
		var uri = await imageService.UploadImageAsync(dto.UserProfile.Id,
													  UserProfilePicturesContainerName,
													  dto.FileBytes);

		await UpdateUserProfilePictureAsync(dto, uri);

		return uri;
	}

	public async Task<string> SetGroupProfilePictureAsync(SetGroupProfilePicture dto)
	{
		var uri = await imageService.UploadImageAsync(dto.Group.Id.ToString("N"),
													  GroupProfilePicturesContainerName,
													  dto.FileBytes);

		await UpdateGroupProfilePictureAsync(dto, uri);

		return uri;
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