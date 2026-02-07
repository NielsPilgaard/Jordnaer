using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Features.Images;
using MassTransit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Jordnaer.Features.DeleteUser;

public interface IDeleteUserService
{
	Task<bool> InitiateDeleteUserAsync(string userId, CancellationToken cancellationToken = default);
	Task<bool> DeleteUserAsync(string userId, string token, CancellationToken cancellationToken = default);
	Task<bool> VerifyTokenAsync(string userId, string token, CancellationToken cancellationToken = default);
}

public class DeleteUserService(
	UserManager<ApplicationUser> userManager,
	ILogger<DeleteUserService> logger,
	IPublishEndpoint publishEndpoint,
	NavigationManager navigationManager,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IDiagnosticContext diagnosticContext,
	IImageService imageService)
	: IDeleteUserService
{
	public const string TokenPurpose = "delete-user";
	public static readonly string TokenProvider = TokenOptions.DefaultEmailProvider;

	public async Task<bool> InitiateDeleteUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		diagnosticContext.Set("UserId", userId);

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is null)
		{
			logger.LogError("Cannot initiate deletion of user, the user has no ApplicationUser");
			return false;
		}

		var baseUri = navigationManager.BaseUri.TrimEnd('/');
		if (string.IsNullOrEmpty(baseUri))
		{
			logger.LogError("No BaseUri found through the NavigationManager. A Delete User Url cannot be created.");
			return false;
		}

		var to = new EmailRecipient { Email = user.Email! };

		var token = await userManager.GenerateUserTokenAsync(user, TokenProvider, TokenPurpose);

		var deletionLink = $"{baseUri}/delete-user/{token}";

		var message = CreateDeleteUserEmailMessage(baseUri, deletionLink);

		var email = new SendEmail
		{
			From = EmailConstants.ContactEmail, // Must be from a verified email
			Subject = "Anmodning om sletning af bruger",
			HtmlContent = message,
			To = [to]
		};

		await publishEndpoint.Publish(email, cancellationToken);

		return true;
	}

	public async Task<bool> DeleteUserAsync(string userId, string token, CancellationToken cancellationToken = default)
	{
		diagnosticContext.Set("UserId", userId);

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is null)
		{
			logger.LogError("Cannot delete user, the user has no ApplicationUser");
			return false;
		}

		var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, token);
		if (tokenIsValid is false)
		{
			logger.LogWarning("The token provided by User {UserId} is not valid for " +
							   "the token purpose {tokenPurpose}, " +
							   "stopping the deletion of the user.", userId, TokenPurpose);
			return false;
		}

		var executionStrategy = context.Database.CreateExecutionStrategy();
		return await executionStrategy.ExecuteAsync(async () =>
		{
			await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
			try
			{
				// TODO: Make sure all user data is deleted here, and that owned groups are assigned new admins
				var identityResult = await userManager.DeleteAsync(user);
				if (identityResult.Succeeded is false)
				{
					logger.LogError("Failed to delete user. Errors: {@identityResultErrors}",
									identityResult.Errors.Select(error => $"ErrorCode {error.Code}: {error.Description}"));

					await transaction.RollbackAsync(cancellationToken);
					return false;
				}

				var modifiedRows = await context.UserProfiles
												.Where(userProfile => userProfile.Id == userId)
												.ExecuteDeleteAsync(cancellationToken);

				if (modifiedRows <= 0)
				{
					logger.LogError("Failed to delete the user profile.");

					await transaction.RollbackAsync(cancellationToken);
					return false;
				}

				await context.SaveChangesAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);

				// Delete all saved images owned by the user
				await imageService.DeleteImageAsync(userId, ProfileImageService.UserProfilePicturesContainerName, cancellationToken);

				var childrenIds = await context.ChildProfiles
											   .Where(child => child.UserProfileId == userId)
											   .Select(child => child.Id)
											   .ToListAsync(cancellationToken);
				foreach (var id in childrenIds)
				{
					await imageService.DeleteImageAsync(id.ToString(), ProfileImageService.ChildProfilePicturesContainerName, cancellationToken);
				}

				logger.LogInformation("User {UserId} has been deleted.", userId);

				await publishEndpoint.Publish(new UserDeleted(userId), cancellationToken);

				return true;
			}
			catch (Exception exception)
			{
				logger.LogException(exception);
				return false;
			}
		});
	}

	public async Task<bool> VerifyTokenAsync(string userId,
		string token,
		CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is not null)
		{
			var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, token);
			return tokenIsValid;
		}

		logger.LogError("Cannot verify user token, the user has no ApplicationUser");
		return false;
	}

	private static string CreateDeleteUserEmailMessage(string baseUrl, string deletionLink) =>
		EmailContentBuilder.DeleteUser(baseUrl, deletionLink);
}

public record UserDeleted(string Id);
