using Jordnaer.Authorization;
using Jordnaer.Database;
using Jordnaer.Shared;
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
	/// <returns></returns>
	Task<OneOf<Success<ProfileDto>, NotFound>> GetUserProfile(string userName);

	/// <summary>
	/// Updates the profile of the user that is currently logged in.
	/// </summary>
	/// <param name="updatedUserProfile">The user profile.</param>
	Task UpdateUserProfile(UserProfile updatedUserProfile);
}

public sealed class ProfileService : IProfileService
{
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory;
	private readonly CurrentUser _currentUser;

	public ProfileService(IDbContextFactory<JordnaerDbContext> contextFactory, CurrentUser currentUser)
	{
		_contextFactory = contextFactory;
		_currentUser = currentUser;
	}

	public async Task<OneOf<Success<ProfileDto>, NotFound>> GetUserProfile(string userName)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
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
				   ? new NotFound()
				   : new Success<ProfileDto>(profile);
	}

	public async Task UpdateUserProfile(UserProfile updatedUserProfile)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();
		var currentUserProfile = await context.UserProfiles
											  .AsSingleQuery()
											  .Include(user => user.Categories)
											  .Include(user => user.ChildProfiles)
											  .FirstOrDefaultAsync(user => user.Id == _currentUser.Id);

		if (currentUserProfile is null)
		{
			currentUserProfile = updatedUserProfile.Map();
			context.UserProfiles.Add(currentUserProfile);
		}
		else
		{
			await UpdateExistingUserProfileAsync(currentUserProfile, updatedUserProfile, context);
			context.Entry(currentUserProfile).State = EntityState.Modified;
		}

		await context.SaveChangesAsync();
	}

	internal static async Task UpdateExistingUserProfileAsync(UserProfile currentUserProfile,
		UserProfile updatedUserProfile,
		JordnaerDbContext context)
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
			var category = await context.Categories.FindAsync(categoryDto.Id);
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
			var childProfile = await context.ChildProfiles.FindAsync(childProfileDto.Id);
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