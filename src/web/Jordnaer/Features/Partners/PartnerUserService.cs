using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Features.Email;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using System.Security.Cryptography;

namespace Jordnaer.Features.Partners;

public interface IPartnerUserService
{
	Task<OneOf<CreatePartnerResult, Error<string>>> CreatePartnerAccountAsync(
		CreatePartnerRequest request,
		CancellationToken cancellationToken = default);

	Task<OneOf<Success, NotFound>> ResendWelcomeEmailAsync(
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
	JordnaerDbContext dbContext,
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
		if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
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
			// Create UserProfile
			var userProfile = new UserProfile { Id = user.Id };
			dbContext.UserProfiles.Add(userProfile);
			await dbContext.SaveChangesAsync(cancellationToken);

			// Assign Partner role
			var roleResult = await userRoleService.AddRoleToUserAsync(user.Id, AppRoles.Partner);
			if (roleResult.IsT2) // Error
			{
				throw new InvalidOperationException("Failed to assign Partner role");
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
				CreatedUtc = DateTime.UtcNow
			};

			dbContext.Partners.Add(partner);
			await dbContext.SaveChangesAsync(cancellationToken);

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
			// Rollback: Delete the user account if partner/profile creation failed
			logger.LogError(ex, "Failed to complete partner account creation for {Email}. Rolling back user account.", new MaskedEmail(request.Email));
			await userManager.DeleteAsync(user);
			throw;
		}
	}

	public async Task<OneOf<Success, NotFound>> ResendWelcomeEmailAsync(
		string userId,
		CancellationToken cancellationToken = default)
	{
		var user = await userManager.FindByIdAsync(userId);
		if (user is null)
		{
			return new NotFound();
		}

		var partner = await dbContext.Partners
			.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

		if (partner is null)
		{
			logger.LogWarning("User {UserId} exists but has no associated partner record", userId);
			return new NotFound();
		}

		// Generate new temporary password
		var newTemporaryPassword = GenerateSecurePassword();

		// Remove old password and set new one
		await userManager.RemovePasswordAsync(user);
		var resetResult = await userManager.AddPasswordAsync(user, newTemporaryPassword);

		if (!resetResult.Succeeded)
		{
			var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
			logger.LogError("Failed to reset password for user {UserId}. Errors: {Errors}", userId, errors);
			throw new InvalidOperationException($"Failed to reset password: {errors}");
		}

		// Resend welcome email
		await emailService.SendPartnerWelcomeEmailAsync(
			user.Email!,
			partner.Name,
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

		for (var group = 0; group < 4; group++)
		{
			if (group > 0)
			{
				password[position++] = '-';
			}

			for (var i = 0; i < 4; i++)
			{
				// Ensure at least one of each character type in the password
				if (position == 0)
				{
					password[position++] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
				}
				else if (position == 2)
				{
					password[position++] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
				}
				else if (position == 4)
				{
					password[position++] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
				}
				else
				{
					password[position++] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
				}
			}
		}

		return new string(password);
	}
}
