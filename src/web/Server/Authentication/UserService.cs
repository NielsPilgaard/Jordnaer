using Jordnaer.Server.Database;
using Jordnaer.Shared.Auth;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Server.Authentication;

public interface IUserService
{
    Task<bool> CreateUserAsync(UserInfo newUser);

    Task<GetOrCreateUserResult> GetOrCreateUserAsync(string provider, ExternalUserInfo newUser);

    Task<bool> IsLoginValidAsync(UserInfo userInfo);
}

public enum GetOrCreateUserResult
{
    UserExists = 0,
    UserCreated = 1,
    FailedToCreateUser = 2,
    FailedToAddLogin = 3
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> CreateUserAsync(UserInfo newUser)
    {
        var user = new ApplicationUser { Email = newUser.Email, UserName = newUser.Email };

        var identityResult = await _userManager.CreateAsync(user, newUser.Password);

        if (identityResult.Succeeded is false)
        {
            _logger.LogWarning("Registration failed. " +
                              "Errors: {@identityResultErrors}", identityResult.Errors);
        }

        return identityResult.Succeeded;
    }

    public async Task<GetOrCreateUserResult> GetOrCreateUserAsync(string provider, ExternalUserInfo newUser)
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

        if (identityResult.Succeeded)
        {
            return GetOrCreateUserResult.UserCreated;
        }

        _logger.LogWarning("Failed to add Login to User {userName}. Errors: {identityResultErrors}",
            newUser.Email,
            identityResult.Errors);

        return GetOrCreateUserResult.FailedToAddLogin;
    }

    public async Task<bool> IsLoginValidAsync(UserInfo userInfo)
    {
        var user = await _userManager.FindByEmailAsync(userInfo.Email);
        if (user is null)
        {
            return false;
        }

        if (_userManager.SupportsUserLockout)
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogInformation("User {userName} tried to login, " +
                                    "but failed because they are currently locked out.", user.UserName);

                return false;
            }
        }

        bool passwordMatches = await _userManager.CheckPasswordAsync(user, userInfo.Password);

        return passwordMatches;
    }
}
