using Jordnaer.Database;
using Jordnaer.Extensions;
using Jordnaer.Features.Email;
using Jordnaer.Features.Images;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using Serilog;

namespace Jordnaer.Features.DeleteUser;

public interface IDeleteUserService
{
	Task<bool> InitiateDeleteUserAsync(string userId, CancellationToken cancellationToken = default);
	Task<bool> DeleteUserAsync(string userId, string token, CancellationToken cancellationToken = default);
	Task<bool> VerifyTokenAsync(string userId, string token, CancellationToken cancellationToken = default);
}

public class DeleteUserService : IDeleteUserService
{
	public const string TokenPurpose = "delete-user";
	public static readonly string TokenProvider = TokenOptions.DefaultEmailProvider;

	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ILogger<DeleteUserService> _logger;
	private readonly ISendGridClient _sendGridClient;
	private readonly IServer _server;
	private readonly IDbContextFactory<JordnaerDbContext> _contextFactory;
	private readonly IDiagnosticContext _diagnosticContext;
	private readonly IImageService _imageService;

	public DeleteUserService(UserManager<ApplicationUser> userManager,
		ILogger<DeleteUserService> logger,
		ISendGridClient sendGridClient,
		IServer server,
		IDbContextFactory<JordnaerDbContext> contextFactory,
		IDiagnosticContext diagnosticContext,
		IImageService imageService)
	{
		_userManager = userManager;
		_logger = logger;
		_sendGridClient = sendGridClient;
		_server = server;
		_contextFactory = contextFactory;
		_diagnosticContext = diagnosticContext;
		_imageService = imageService;
	}

	public async Task<bool> InitiateDeleteUserAsync(string userId, CancellationToken cancellationToken = default)
	{
		_diagnosticContext.Set("UserId", userId);

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

		var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is null)
		{
			_logger.LogError("Cannot initiate deletion of user, the user has no ApplicationUser");
			return false;
		}

		var serverAddressFeature = _server.Features.Get<IServerAddressesFeature>();
		var serverAddress = serverAddressFeature?.Addresses.FirstOrDefault();
		if (serverAddress is null)
		{
			_logger.LogError("No addresses found in the IServerAddressFeature. A Delete User Url cannot be created.");
			return false;
		}

		var to = new EmailAddress(user.Email);

		var token = await _userManager.GenerateUserTokenAsync(user, TokenProvider, TokenPurpose);

		var deletionLink = $"{serverAddress}/delete-user/{token}";

		var message = CreateDeleteUserEmailMessage(deletionLink);

		var email = new SendGridMessage
		{
			From = EmailConstants.ContactEmail, // Must be from a verified email
			Subject = "Anmodning om sletning af bruger",
			HtmlContent = message
		};

		email.AddTo(to);
		email.TrackingSettings = new TrackingSettings
		{
			ClickTracking = new ClickTracking { Enable = false },
			Ganalytics = new Ganalytics { Enable = false },
			OpenTracking = new OpenTracking { Enable = false },
			SubscriptionTracking = new SubscriptionTracking { Enable = false }
		};

		// TODO: Turn this email sending into an Azure Function
		var emailSentResponse = await _sendGridClient.SendEmailAsync(email, cancellationToken);

		return emailSentResponse.IsSuccessStatusCode;
	}

	public async Task<bool> DeleteUserAsync(string userId, string token, CancellationToken cancellationToken = default)
	{
		_diagnosticContext.Set("UserId", userId);

		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

		var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is null)
		{
			_logger.LogError("Cannot delete user, the user has no ApplicationUser");
			return false;
		}

		var tokenIsValid = await _userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, token);
		if (tokenIsValid is false)
		{
			_logger.LogWarning("The token {token} is not valid for the token purpose {tokenPurpose}, " +
							   "stopping the deletion of the user.", token, TokenPurpose);
			return false;
		}

		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
		try
		{
			// TODO: Make sure all user data is deleted here, and that owned groups are assigned new admins
			var identityResult = await _userManager.DeleteAsync(user);
			if (identityResult.Succeeded is false)
			{
				_logger.LogError("Failed to delete user. Errors: {@identityResultErrors}",
					 identityResult.Errors.Select(error => $"ErrorCode {error.Code}: {error.Description}"));

				await transaction.RollbackAsync(cancellationToken);
				return false;
			}

			var modifiedRows = await context.UserProfiles
											.Where(userProfile => userProfile.Id == userId)
											.ExecuteDeleteAsync(cancellationToken);

			if (modifiedRows <= 0)
			{
				_logger.LogError("Failed to delete the user profile.");

				await transaction.RollbackAsync(cancellationToken);
				return false;
			}

			await context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			// Delete all saved images owned by the user
			await _imageService.DeleteImageAsync(userId, ProfileImageService.UserProfilePicturesContainerName, cancellationToken);

			var childrenIds = await context.ChildProfiles
										   .Where(child => child.UserProfileId == userId)
										   .Select(child => child.Id)
										   .ToListAsync(cancellationToken);
			foreach (var id in childrenIds)
			{
				await _imageService.DeleteImageAsync(id.ToString(), ProfileImageService.ChildProfilePicturesContainerName, cancellationToken);
			}

			return true;
		}
		catch (Exception exception)
		{
			await transaction.RollbackAsync(cancellationToken);
			_logger.LogException(exception);
			return false;
		}
	}

	public async Task<bool> VerifyTokenAsync(string userId,
		string token,
		CancellationToken cancellationToken = default)
	{
		await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

		var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
		if (user is not null)
		{
			var tokenIsValid = await _userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, token);
			return tokenIsValid;
		}

		_logger.LogError("Cannot verify user token, the user has no ApplicationUser");
		return false;
	}

	private static string CreateDeleteUserEmailMessage(string deletionLink) =>
		$"""

		 <p>Hej,</p>

		 <p>Du har anmodet om at slette din bruger hos Mini Møder. Hvis du fortsætter, vil alle dine data blive permanent slettet og kan ikke genoprettes.</p>

		 <p>Hvis du er sikker på, at du vil slette din bruger, skal du klikke på linket nedenfor:</p>

		 <p><a href="{deletionLink}">Bekræft sletning af bruger</a></p>

		 <p>Hvis du ikke anmodede om at slette din bruger, kan du ignorere denne e-mail.</p>

		 <p>Venlig hilsen,</p>

		 <p>Mini Møder teamet</p>

		 """;
}

