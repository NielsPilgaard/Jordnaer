using Jordnaer.Database;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Authentication;

public interface IUserService
{
	/// <summary>
	/// Creates a new <see cref="ApplicationUser"/> and <see cref="UserProfile"/>.
	/// </summary>
	/// <param name="newUser"></param>
	/// <param name="cancellationToken"></param>
	/// <returns><c>null</c> if the operation failed, the id of the created user if the operation succeeded</returns>
	Task<string?> CreateUserAsync(UserInfo newUser, CancellationToken cancellationToken = default);

	Task<GetOrCreateUserResult> GetOrCreateUserAsync(string provider, ExternalUserInfo newUser, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if the login provided through <paramref name="userInfo"/> is valid.
	/// </summary>
	/// <param name="userInfo"></param>
	/// <param name="cancellationToken"></param>
	/// <returns><c>null</c> if the operation failed, the id of the created user if the operation succeeded</returns>
	Task<string?> IsLoginValidAsync(UserInfo userInfo, CancellationToken cancellationToken = default);
}

public enum GetOrCreateUserResult
{
	UserExists = 0,
	UserCreated = 1,
	FailedToCreateUser = 2,
	FailedToAddLogin = 3,
	Exception = 4
}

public class UserService : IUserService
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ILogger<UserService> _logger;
	private readonly JordnaerDbContext _context;

	public UserService(UserManager<ApplicationUser> userManager, ILogger<UserService> logger, JordnaerDbContext context)
	{
		_userManager = userManager;
		_logger = logger;
		_context = context;
	}

	public async Task<string?> CreateUserAsync(UserInfo newUser, CancellationToken cancellationToken = default)
	{
		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			string userId = Guid.NewGuid().ToString();
			var user = new ApplicationUser { Email = newUser.Email, UserName = newUser.Email, Id = userId };

			var identityResult = await _userManager.CreateAsync(user, newUser.Password);

			if (identityResult.Succeeded is false)
			{
				_logger.LogWarning("Registration failed. " +
								   "Errors: {@identityResultErrors}", identityResult.Errors);
				return null;
			}

			_context.UserProfiles.Add(new UserProfile { Id = userId });

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return userId;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Exception occurred in the {functionName} function.", nameof(CreateUserAsync));
			await transaction.RollbackAsync(cancellationToken);
			return null;
		}
	}

	public async Task<GetOrCreateUserResult> GetOrCreateUserAsync(string provider,
		ExternalUserInfo newUser,
		CancellationToken cancellationToken = default)
	{
		await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

		try
		{
			var user = await _userManager.FindByLoginAsync(provider, newUser.ProviderKey);
			if (user is not null)
			{
				return GetOrCreateUserResult.UserExists;
			}

			user = new ApplicationUser { UserName = newUser.Email, Email = newUser.Email, Id = newUser.ProviderKey };

			var identityResult = await _userManager.CreateAsync(user);
			if (!identityResult.Succeeded)
			{
				_logger.LogWarning("Failed to create User {userName}. Errors: {@identityResultErrors}",
					newUser.Email,
					identityResult.Errors);

				return GetOrCreateUserResult.FailedToCreateUser;
			}

			identityResult = await _userManager.AddLoginAsync(
				user,
				new UserLoginInfo(provider, newUser.ProviderKey, displayName: null));

			if (!identityResult.Succeeded)
			{
				_logger.LogWarning("Failed to add Login to User {userName}. Errors: {identityResultErrors}",
					newUser.Email,
					identityResult.Errors);

				return GetOrCreateUserResult.FailedToAddLogin;
			}

			_context.UserProfiles.Add(new UserProfile { Id = newUser.ProviderKey });

			await _context.SaveChangesAsync(cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return GetOrCreateUserResult.UserCreated;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Exception occurred in the {functionName} function.", nameof(GetOrCreateUserAsync));
			await transaction.RollbackAsync(cancellationToken);
			return GetOrCreateUserResult.Exception;
		}
	}

	public async Task<string?> IsLoginValidAsync(UserInfo userInfo, CancellationToken cancellationToken = default)
	{
		var user = await _userManager.FindByEmailAsync(userInfo.Email);
		if (user is null)
		{
			return null;
		}

		if (_userManager.SupportsUserLockout)
		{
			if (await _userManager.IsLockedOutAsync(user))
			{
				_logger.LogInformation("User {userName} tried to login, " +
									"but failed because they are currently locked out.", user.UserName);

				return null;
			}
		}

		bool passwordMatches = await _userManager.CheckPasswordAsync(user, userInfo.Password);
		return passwordMatches
			? user.Id
			: null;
	}
}
