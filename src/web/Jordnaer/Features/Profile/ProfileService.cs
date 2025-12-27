using Jordnaer.Database;
using Jordnaer.Features.Authentication;
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
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<OneOf<Success<ProfileDto>, NotFound>> GetUserProfile(string userName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates the profile of the user that is currently logged in.
	/// </summary>
	/// <param name="updatedUserProfile">The user profile.</param>
	/// <param name="cancellationToken"></param>
	ValueTask<OneOf<Success<UserProfile>, Error>> UpdateUserProfile(UserProfile updatedUserProfile, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if the user profile is complete (has required fields).
	/// </summary>
	/// <param name="userId">The user ID to check.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>True if profile is complete, false otherwise.</returns>
	Task<bool> IsProfileCompleteAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates a unique username from first and last name.
	/// </summary>
	/// <param name="firstName">First name.</param>
	/// <param name="lastName">Last name.</param>
	/// <param name="cancellationToken"></param>
	/// <returns>A unique username or error message.</returns>
	Task<OneOf<Success<string>, Error<string>>> GenerateUniqueUsernameAsync(string firstName, string lastName, CancellationToken cancellationToken = default);
}

public sealed class ProfileService(
	IDbContextFactory<JordnaerDbContext> contextFactory,
	CurrentUser currentUser) : IProfileService
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

	public async ValueTask<OneOf<Success<UserProfile>, Error>> UpdateUserProfile(UserProfile updatedUserProfile, CancellationToken cancellationToken = default)
	{
		if (currentUser.Id is null)
		{
			return new Error();
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var currentUserProfile = await context.UserProfiles
											  .AsSingleQuery()
											  .Include(user => user.Categories)
											  .Include(user => user.ChildProfiles)
											  .FirstOrDefaultAsync(user => user.Id == currentUser.Id,
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

		return new Success<UserProfile>(currentUserProfile);
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
		currentUserProfile.Location = updatedUserProfile.Location;
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

	public async Task<bool> IsProfileCompleteAsync(string userId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var profile = await context.UserProfiles
									.AsNoTracking()
									.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

		if (profile is null)
		{
			return false;
		}

		// Basic completeness check
		return !string.IsNullOrWhiteSpace(profile.FirstName) &&
			   !string.IsNullOrWhiteSpace(profile.LastName) &&
			   profile.Location is not null &&
			   (profile.ZipCode.HasValue || !string.IsNullOrWhiteSpace(profile.Address));
	}

	public async Task<OneOf<Success<string>, Error<string>>> GenerateUniqueUsernameAsync(string firstName, string lastName, CancellationToken cancellationToken = default)
	{
		var baseUsername = $"{firstName}{lastName}".ToLowerInvariant()
			.Replace(" ", "")
			.Replace("æ", "ae")
			.Replace("ø", "oe")
			.Replace("å", "aa");

		// Remove non-alphanumeric characters
		baseUsername = new string(baseUsername.Where(c => char.IsLetterOrDigit(c)).ToArray());

		if (string.IsNullOrWhiteSpace(baseUsername))
		{
			return new Error<string>("Kunne ikke generere brugernavn");
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// Fetch all existing usernames that start with baseUsername in one query (case-insensitive)
		var pattern = $"{baseUsername}%";
		var existingUsernames = await context.UserProfiles
			.AsNoTracking()
			.Where(p => p.UserName != null && EF.Functions.Like(p.UserName, pattern))
			.Select(p => p.UserName)
			.ToHashSetAsync(cancellationToken);

		var username = baseUsername;
		if (!existingUsernames.Any(u => string.Equals(u, username, StringComparison.OrdinalIgnoreCase)))
		{
			return new Success<string>(username);
		}

		// Find first available counter
		for (var counter = 2; counter <= 1000; counter++)
		{
			username = $"{baseUsername}{counter}";
			if (!existingUsernames.Contains(username, StringComparer.OrdinalIgnoreCase))
			{
				return new Success<string>(username);
			}
		}

		return new Error<string>("Kunne ikke finde et unikt brugernavn");
	}
}