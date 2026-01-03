using Jordnaer.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Jordnaer.Features.Authentication;

public interface IUserRoleService
{
	Task<List<UserRoleDto>> GetAllUsersWithRolesAsync();
	Task<OneOf<Success, NotFound, Error<string>>> AddRoleToUserAsync(string userId, string roleName);
	Task<OneOf<Success, NotFound, Error<string>>> RemoveRoleFromUserAsync(string userId, string roleName);
	Task<List<string>> GetUserRolesAsync(string userId);
}

public class UserRoleDto
{
	public string UserId { get; set; } = string.Empty;
	public string UserName { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public List<string> Roles { get; set; } = [];
}

public sealed class UserRoleService(
	UserManager<ApplicationUser> userManager,
	RoleManager<IdentityRole> roleManager,
	ILogger<UserRoleService> logger,
	CurrentUser currentUser,
	JordnaerDbContext dbContext) : IUserRoleService
{
	private static readonly string[] AllowedRoles = [AppRoles.Admin, AppRoles.Partner];

	// TODO: This needs pagination when we have many users
	public async Task<List<UserRoleDto>> GetAllUsersWithRolesAsync()
	{
		// Load all users
		var users = await userManager.Users.ToListAsync();
		var userIds = users.Select(u => u.Id).ToList();

		// Batch-load all role assignments for these users in a single query
		var rolesByUserId = await dbContext.UserRoles
				.Join(dbContext.Roles,
					  userRole => userRole.RoleId,
					  role => role.Id,
					  (userRole, role) => new { userRole.UserId, RoleName = role.Name })
				.Where(userRole => userIds.Contains(userRole.UserId) &&
									!string.IsNullOrEmpty(userRole.RoleName) &&
									AllowedRoles.Contains(userRole.RoleName))
				.Select(userRole => new { userRole.UserId, RoleName = userRole.RoleName! })
				.GroupBy(userRole => new { userRole.UserId })
				.ToDictionaryAsync(g => g.Key.UserId, g => g.Select(ur => ur.RoleName).ToList());

		// Build DTOs using the pre-loaded role data
		var userDtos = users.Select(user => new UserRoleDto
		{
			UserId = user.Id,
			UserName = user.UserName ?? string.Empty,
			Email = MaskEmail(user.Email ?? string.Empty),
			Roles = rolesByUserId.TryGetValue(user.Id, out var roles) ? roles : []
		}).ToList();

		return userDtos;
	}

	public async Task<OneOf<Success, NotFound, Error<string>>> AddRoleToUserAsync(string userId, string roleName)
	{
		if (!AllowedRoles.Contains(roleName))
		{
			logger.LogWarning("Attempt to add invalid role {RoleName} by admin {AdminId}", roleName, currentUser.Id);
			return new Error<string>("Invalid role");
		}

		var user = await userManager.FindByIdAsync(userId);
		if (user == null)
		{
			return new NotFound();
		}

		// Ensure the role exists
		if (!await roleManager.RoleExistsAsync(roleName))
		{
			var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
			if (!roleResult.Succeeded)
			{
				logger.LogError("Failed to create role {RoleName}. Errors: {Errors}",
					roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
				return new Error<string>("Failed to create role");
			}
		}

		// Check if user already has the role
		if (await userManager.IsInRoleAsync(user, roleName))
		{
			return new Success();
		}

		var result = await userManager.AddToRoleAsync(user, roleName);
		if (result.Succeeded)
		{
			logger.LogInformation(
				"Admin {AdminId} added role {RoleName} to user {UserId} at {Timestamp}",
				currentUser.Id,
				roleName,
				userId,
				DateTime.UtcNow);
			return new Success();
		}

		logger.LogError("Failed to add role {RoleName} to user {UserId}. Errors: {Errors}",
			roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
		return new Error<string>("Failed to add role");
	}

	public async Task<OneOf<Success, NotFound, Error<string>>> RemoveRoleFromUserAsync(string userId, string roleName)
	{
		if (!AllowedRoles.Contains(roleName))
		{
			logger.LogWarning("Attempt to remove invalid role {RoleName} by admin {AdminId}", roleName, currentUser.Id);
			return new Error<string>("Invalid role");
		}

		// Prevent self-lockout - admin cannot remove their own admin role
		if (userId == currentUser.Id && roleName == AppRoles.Admin)
		{
			logger.LogWarning("Admin {AdminId} attempted to remove their own admin role", currentUser.Id);
			return new Error<string>("Cannot remove your own admin role");
		}

		var user = await userManager.FindByIdAsync(userId);
		if (user == null)
		{
			return new NotFound();
		}

		var result = await userManager.RemoveFromRoleAsync(user, roleName);
		if (result.Succeeded)
		{
			logger.LogInformation(
				"Admin {AdminId} removed role {RoleName} from user {UserId} at {Timestamp}",
				currentUser.Id,
				roleName,
				userId,
				DateTime.UtcNow);
			return new Success();
		}

		logger.LogError("Failed to remove role {RoleName} from user {UserId}. Errors: {Errors}",
			roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
		return new Error<string>("Failed to remove role");
	}

	public async Task<List<string>> GetUserRolesAsync(string userId)
	{
		var user = await userManager.FindByIdAsync(userId);
		if (user == null)
		{
			return [];
		}

		var roles = await userManager.GetRolesAsync(user);
		return roles.Where(r => AllowedRoles.Contains(r)).ToList();
	}

	private static string MaskEmail(string email)
	{
		var atIndex = email?.LastIndexOf('@') ?? -1;
		if (string.IsNullOrEmpty(email) || atIndex <= 0)
		{
			return email ?? string.Empty;
		}

		var localPart = email[..atIndex];
		var domain = email[(atIndex + 1)..];

		if (localPart.Length <= 2)
		{
			return $"{localPart[0]}***@{domain}";
		}

		var visibleChars = Math.Max(1, localPart.Length / 3);
		var masked = localPart[..visibleChars] + new string('*', localPart.Length - visibleChars);
		return $"{masked}@{domain}";
	}
}
