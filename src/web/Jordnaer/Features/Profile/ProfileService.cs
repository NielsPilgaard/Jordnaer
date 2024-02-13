using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using NotFound = OneOf.Types.NotFound;

namespace Jordnaer.Features.Profile;

public interface IProfileService
{
	/// <summary>
	/// Gets the user profile that matches <paramref name="userName"/>.
	/// <para>
	/// If no such user profile exists, <c>404 Not Found</c> is returned.
	/// </para>
	/// </summary>
	/// <param name="userName">Name of the user.</param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<OneOf<Success<ProfileDto>, NotFound>> GetUserProfile(string userName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates the profile of the user that is currently logged in.
	/// </summary>
	/// <param name="updatedUserProfile">The user profile.</param>
	/// <param name="cancellationToken"></param>
	Task<OneOf<Success, Error>> UpdateUserProfile(UserProfile updatedUserProfile, CancellationToken cancellationToken = default);
}

public sealed class ProfileService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	AuthenticationStateProvider authenticationStateProvider) : IProfileService
{
	public async Task<OneOf<Success<ProfileDto>, NotFound>> GetUserProfile(string userName,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var profile = await context
							.UserProfiles
							.AsNoTracking()
							.AsSingleQuery()
							.Include(userProfile => userProfile.ChildProfiles)
							.Include(userProfile => userProfile.Categories)
							.Where(userProfile => userProfile.UserName == userName)
							.Select(userProfile => userProfile.ToProfileDto())
							.FirstOrDefaultAsync(cancellationToken: cancellationToken);

		return profile is null
				   ? new NotFound()
				   : new Success<ProfileDto>(profile);
	}

	public async Task<OneOf<Success, Error>> UpdateUserProfile(UserProfile updatedUserProfile, CancellationToken cancellationToken = default)
	{
		var currentUserId = await authenticationStateProvider.GetCurrentUserId();
		if (currentUserId is null)
		{
			return new Error();
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var currentUserProfile = await context.UserProfiles
											  .AsSingleQuery()
											  .Include(user => user.Categories)
											  .Include(user => user.ChildProfiles)
											  .FirstOrDefaultAsync(user => user.Id == currentUserId,
																   cancellationToken);

		if (currentUserProfile is null)
		{
			currentUserProfile = updatedUserProfile.Map();
			context.UserProfiles.Add(currentUserProfile);
		}
		else
		{
			await UpdateExistingUserProfileAsync(currentUserProfile, updatedUserProfile, context, cancellationToken);
			context.Entry(currentUserProfile).State = EntityState.Modified;
		}

		await context.SaveChangesAsync(cancellationToken);

		return new Success();
	}

	internal static async Task UpdateExistingUserProfileAsync(UserProfile currentUserProfile,
		UserProfile updatedUserProfile,
		JordnaerDbContext context,
		CancellationToken cancellationToken = default)
	{
		currentUserProfile.FirstName = updatedUserProfile.FirstName;
		currentUserProfile.LastName = updatedUserProfile.LastName;
		currentUserProfile.Address = updatedUserProfile.Address;
		currentUserProfile.City = updatedUserProfile.City;
		currentUserProfile.ZipCode = updatedUserProfile.ZipCode;
		currentUserProfile.DateOfBirth = updatedUserProfile.DateOfBirth;
		currentUserProfile.Description = updatedUserProfile.Description;
		currentUserProfile.PhoneNumber = updatedUserProfile.PhoneNumber;
		currentUserProfile.ProfilePictureUrl = updatedUserProfile.ProfilePictureUrl;
		currentUserProfile.UserName = updatedUserProfile.UserName;

		currentUserProfile.Categories.Clear();
		foreach (var categoryDto in updatedUserProfile.Categories)
		{
			var category = await context.Categories.FindAsync([categoryDto.Id], cancellationToken);
			if (category is null)
			{
				currentUserProfile.Categories.Add(categoryDto);
				context.Entry(categoryDto).State = EntityState.Added;
			}
			else
			{
				category.LoadValuesFrom(categoryDto);
				currentUserProfile.Categories.Add(category);
				context.Entry(category).State = EntityState.Modified;
			}
		}

		currentUserProfile.ChildProfiles.Clear();
		foreach (var childProfileDto in updatedUserProfile.ChildProfiles)
		{
			var childProfile = await context.ChildProfiles.FindAsync([childProfileDto.Id], cancellationToken);
			if (childProfile is null)
			{
				currentUserProfile.ChildProfiles.Add(childProfileDto);
				context.Entry(childProfileDto).State = EntityState.Added;
			}
			else
			{
				childProfile.LoadValuesFrom(childProfileDto);
				currentUserProfile.ChildProfiles.Add(childProfile);
				context.Entry(childProfile).State = EntityState.Modified;
			}
		}
	}
}