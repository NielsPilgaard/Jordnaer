using Jordnaer.Server.Data;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Identity;
using ILogger = Serilog.ILogger;

namespace Jordnaer.Server.Authentication;

public interface IUserService
{
    Task<bool> CreateUserAsync(UserInfo newUser);

    Task<bool> IsLoginValidAsync(UserInfo userInfo);

    Task<bool> DeleteUserAsync(ApplicationUser user);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger _logger;

    public UserService(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        _userManager = userManager;
        _logger = logger.ForContext<UserService>();
    }

    public async Task<bool> CreateUserAsync(UserInfo newUser)
    {
        var user = new ApplicationUser { Email = newUser.Email, UserName = newUser.Email };

        var identityResult = await _userManager.CreateAsync(user, newUser.Password);
        if (identityResult.Succeeded is false)
        {
            _logger.Warning("Registration failed. " +
                              "UserInfo: {@userInfo}. " +
                              "Errors: {@identityResultErrors}", newUser, identityResult.Errors);
        }

        return identityResult.Succeeded;
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
                _logger.Information("User {userName} tried to login, " +
                                    "but failed because they are currently locked out.", user.UserName);

                return false;
            }
        }

        bool passwordMatches = await _userManager.CheckPasswordAsync(user, userInfo.Password);

        return passwordMatches;
    }


    public async Task<bool> DeleteUserAsync(ApplicationUser user)
    {
        var identityResult = await _userManager.DeleteAsync(user);
        if (identityResult.Succeeded is false)
        {
            _logger.Warning("Failed to delete user {userEmail}. Errors: {@identityResultErrors}",
                user.Email, identityResult.Errors);
        }

        return identityResult.Succeeded;
    }
}
