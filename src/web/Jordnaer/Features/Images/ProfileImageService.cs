using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Features.Images;

public interface IProfileImageService
{
	Task<string> SetChildProfilePictureAsync(SetChildProfilePicture dto);
	Task<string> SetUserProfilePictureAsync(SetUserProfilePicture dto);
	Task<string> SetGroupProfilePictureAsync(SetGroupProfilePicture dto);
}

public class ProfileImageService(IDbContextFactory<JordnaerDbContext> contextFactory, IImageService imageService) : IProfileImageService
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

		// Only update the database if the group already exists
		// For new groups, the ProfilePictureUrl will be saved when the group is created
		await using var context = await contextFactory.CreateDbContextAsync();
		var groupExists = await context.Groups.AnyAsync(g => g.Id == dto.Group.Id);
		if (groupExists)
		{
			await UpdateGroupProfilePictureAsync(dto, uri);
		}

		return uri;
	}

	private async Task UpdateChildProfilePictureAsync(
		SetChildProfilePicture dto,
		string uri,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var currentChildProfile = await context.ChildProfiles.FindAsync([dto.ChildProfile.Id], cancellationToken);
		if (currentChildProfile is null)
		{
			dto.ChildProfile.PictureUrl = uri;
			context.ChildProfiles.Add(dto.ChildProfile);
			await context.SaveChangesAsync(cancellationToken);
			return;
		}

		// Updating is only required if the pictureUrl is not already correct
		if (currentChildProfile.PictureUrl != uri)
		{
			currentChildProfile.PictureUrl = uri;
			context.Entry(currentChildProfile).State = EntityState.Modified;

			await context.SaveChangesAsync(cancellationToken);
		}
	}

	private async Task UpdateUserProfilePictureAsync(
		SetUserProfilePicture dto,
		string uri,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var currentUserProfile = await context.UserProfiles.FindAsync([dto.UserProfile.Id], cancellationToken);
		if (currentUserProfile is null)
		{
			dto.UserProfile.ProfilePictureUrl = uri;
			context.UserProfiles.Add(dto.UserProfile);
			await context.SaveChangesAsync(cancellationToken);
			return;
		}

		// Updating is only required if the pictureUrl is not already correct
		if (currentUserProfile.ProfilePictureUrl != uri)
		{
			currentUserProfile.ProfilePictureUrl = uri;
			context.Entry(currentUserProfile).State = EntityState.Modified;

			await context.SaveChangesAsync(cancellationToken);
		}
	}
	private async Task UpdateGroupProfilePictureAsync(
		SetGroupProfilePicture dto,
		string uri,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var currentGroup = await context.Groups.FindAsync([dto.Group.Id], cancellationToken);

		// Group should already exist when this method is called
		if (currentGroup is null)
		{
			throw new InvalidOperationException($"Group with ID {dto.Group.Id} does not exist.");
		}

		// Updating is only required if the pictureUrl is not already correct
		if (currentGroup.ProfilePictureUrl != uri)
		{
			currentGroup.ProfilePictureUrl = uri;
			context.Entry(currentGroup).State = EntityState.Modified;

			await context.SaveChangesAsync(cancellationToken);
		}
	}
}