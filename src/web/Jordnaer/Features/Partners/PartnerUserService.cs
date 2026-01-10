using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using System.Net.Mail;
using System.Security.Cryptography;

namespace Jordnaer.Features.Partners;

public interface IPartnerUserService
{
	Task<OneOf<CreatePartnerResult, Error<string>>> CreatePartnerAccountAsync(
		CreatePartnerRequest request,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, NotFound, Error<string>>> ResendWelcomeEmailAsync(
		string userId,
		CancellationToken cancellationToken = default);
}

public record CreatePartnerRequest
{
	public required string Name { get; init; }
	public required string Email { get; init; }
	public required string Description { get; init; }
	public string? LogoUrl { get; init; }
	public required string Link { get; init; }
	public bool CanHavePartnerCard { get; init; } = true;
	public bool CanHaveAd { get; init; } = true;
}

public record CreatePartnerResult
{
	public required string UserId { get; init; }
	public required Guid PartnerId { get; init; }
	public required string TemporaryPassword { get; init; }
	public required string Email { get; init; }
}

public sealed class PartnerUserService(
	UserManager<ApplicationUser> userManager,
	IUserRoleService userRoleService,
	IDbContextFactory<JordnaerDbContext> contextFactory,
	IEmailService emailService,
	ILogger<PartnerUserService> logger) : IPartnerUserService
{
	private const int TemporaryPasswordLength = 16;

	public async Task<OneOf<CreatePartnerResult, Error<string>>> CreatePartnerAccountAsync(
		CreatePartnerRequest request,
		CancellationToken cancellationToken = default)
	{
		// Check if user with this email already exists
		var existingUser = await userManager.FindByEmailAsync(request.Email);
		if (existingUser is not null)
		{
			logger.LogWarning("Attempted to create partner account with existing email: {Email}", new MaskedEmail(request.Email));
			return new Error<string>("En bruger med denne email findes allerede");
		}

		// Validate email format
		if (!IsValidEmail(request.Email))
		{
			return new Error<string>("Ugyldig email adresse");
		}

		// Validate URL format
		if (!Uri.TryCreate(request.Link, UriKind.Absolute, out var uri) ||
			(uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
		{
			return new Error<string>("Ugyldig link URL");
		}

		// Generate secure temporary password
		var temporaryPassword = GenerateSecurePassword();

		// Create ApplicationUser
		var user = new ApplicationUser
		{
			UserName = request.Email,
			Email = request.Email,
			EmailConfirmed = true // Auto-confirm admin-created accounts
		};

		var createResult = await userManager.CreateAsync(user, temporaryPassword);
		if (!createResult.Succeeded)
		{
			var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
			logger.LogError("Failed to create partner user account. Errors: {Errors}", errors);
			return new Error<string>($"Kunne ikke oprette brugerkonto: {errors}");
		}

		try
		{
			await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

			// Create UserProfile
			var userProfile = new UserProfile { Id = user.Id };
			context.UserProfiles.Add(userProfile);
			await context.SaveChangesAsync(cancellationToken);

			// Assign Partner role
			var roleResult = await userRoleService.AddRoleToUserAsync(user.Id, AppRoles.Partner);
			if (roleResult.IsT1) // NotFound
			{
				throw new InvalidOperationException($"User with ID '{user.Id}' was not found when assigning Partner role");
			}
			if (roleResult.IsT2) // Error
			{
				var error = roleResult.AsT2;
				throw new InvalidOperationException($"Failed to assign Partner role: {error.Value}");
			}
			if (!roleResult.IsT0) // Ensure success before proceeding
			{
				throw new InvalidOperationException("Unexpected result when assigning Partner role");
			}

			// Create Partner record
			var partner = new Partner
			{
				Id = Guid.NewGuid(),
				Name = request.Name,
				Description = request.Description,
				LogoUrl = request.LogoUrl,
				Link = request.Link,
				UserId = user.Id,
				CanHavePartnerCard = request.CanHavePartnerCard,
				CanHaveAd = request.CanHaveAd
			};

			context.Partners.Add(partner);
			await context.SaveChangesAsync(cancellationToken);

			// Send welcome email
			await emailService.SendPartnerWelcomeEmailAsync(
				user.Email,
				request.Name,
				temporaryPassword,
				cancellationToken);

			logger.LogInformation(
				"Created partner account for {Email} with PartnerId {PartnerId}",
				new MaskedEmail(request.Email),
				partner.Id);

			return new CreatePartnerResult
			{
				UserId = user.Id,
				PartnerId = partner.Id,
				TemporaryPassword = temporaryPassword,
				Email = user.Email
			};
		}
		catch (Exception ex)
		{
			// Rollback: Delete the user account and profile if partner/profile creation failed
			logger.LogError(ex, "Failed to complete partner account creation for {Email}. Rolling back user account.", new MaskedEmail(request.Email));

			try
			{
				await using var rollbackContext = await contextFactory.CreateDbContextAsync(cancellationToken);
				var profile = await rollbackContext.UserProfiles.FindAsync([user.Id], cancellationToken);
				if (profile is not null)
				{
					rollbackContext.UserProfiles.Remove(profile);
					await rollbackContext.SaveChangesAsync(cancellationToken);
				}
			}
			catch (Exception cleanupEx)
			{
				logger.LogError(cleanupEx, "Failed to cleanup UserProfile during rollback for {Email}", new MaskedEmail(request.Email));
			}

			var deleteResult = await userManager.DeleteAsync(user);
			if (!deleteResult.Succeeded)
			{
				var deleteErrors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
				logger.LogError("Failed to delete user during rollback for {Email}. Errors: {Errors}", new MaskedEmail(request.Email), deleteErrors);
			}

			return new Error<string>($"Kunne ikke fuldføre oprettelse: {ex.Message}");
		}
	}

	public async Task<OneOf<Success, NotFound, Error<string>>> ResendWelcomeEmailAsync(
		string userId,
		CancellationToken cancellationToken = default)
	{
		var user = await userManager.FindByIdAsync(userId);
		if (user is null)
		{
			return new NotFound();
		}

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var partner = await context.Partners
			.AsNoTracking()
			.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

		if (partner is null)
		{
			logger.LogWarning("User {UserId} exists but has no associated partner record", userId);
			return new NotFound();
		}

		// Generate new temporary password
		var newTemporaryPassword = GenerateSecurePassword();

		// Use atomic password reset with token
		var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
		var resetResult = await userManager.ResetPasswordAsync(user, resetToken, newTemporaryPassword);

		if (!resetResult.Succeeded)
		{
			var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
			logger.LogError("Failed to reset password for user {UserId}. Errors: {Errors}", userId, errors);
			return new Error<string>($"Kunne ikke nulstille kodeord: {errors}");
		}

		// Resend welcome email with null-safe partner name
		var partnerName = partner.Name ?? "Partner";
		await emailService.SendPartnerWelcomeEmailAsync(
			user.Email!,
			partnerName,
			newTemporaryPassword,
			cancellationToken);

		logger.LogInformation("Resent welcome email to partner {Email}", new MaskedEmail(user.Email));

		return new Success();
	}

	private static string GenerateSecurePassword()
	{
		// Generate a cryptographically secure random password
		// Format: 4 groups of 4 characters separated by hyphens (e.g., "aBc4-Xy7z-Mn2P-Qr9T")
		const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Removed I, O
		const string lowercase = "abcdefghijkmnpqrstuvwxyz"; // Removed l, o
		const string digits = "23456789"; // Removed 0, 1
		const string allChars = uppercase + lowercase + digits;

		var password = new char[TemporaryPasswordLength + 3]; // +3 for hyphens
		var position = 0;
		var charIndex = 0; // Track character index without hyphens

		for (var group = 0; group < 4; group++)
		{
			if (group > 0)
			{
				password[position++] = '-';
			}

			for (var i = 0; i < 4; i++)
			{
				// Ensure at least one of each character type in the password
				// charIndex 0: uppercase, charIndex 1: lowercase, charIndex 2: digit
				if (charIndex == 0)
				{
					password[position++] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
				}
				else if (charIndex == 1)
				{
					password[position++] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
				}
				else if (charIndex == 2)
				{
					password[position++] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
				}
				else
				{
					password[position++] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
				}

				charIndex++;
			}
		}

		return new string(password);
	}

	private static bool IsValidEmail(string? email)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return false;
		}

		try
		{
			var mailAddress = new MailAddress(email);
			return mailAddress.Address == email;
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
