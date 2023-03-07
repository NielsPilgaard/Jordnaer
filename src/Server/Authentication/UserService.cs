using Microsoft.AspNetCore.Identity;
using RemindMeApp.Server.Data;
using RemindMeApp.Shared;

namespace RemindMeApp.Server.Authentication;

public interface IUserService
{
    Task<bool> CreateUserAsync(UserInfo newUser);

    Task<bool> LoginIsValid(UserInfo userInfo);

    Task<bool> GetOrCreateUserAsync(string provider, ExternalUserInfo userInfo);
}

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> CreateUserAsync(UserInfo newUser)
    {
        var identityResult = await _userManager.CreateAsync(new ApplicationUser { UserName = newUser.Username }, newUser.Password);

        return identityResult.Succeeded;
    }

    public async Task<bool> LoginIsValid(UserInfo userInfo)
    {
        var user = await _userManager.FindByNameAsync(userInfo.Username);
        if (user is null)
        {
            return false;
        }

        bool passwordMatches = await _userManager.CheckPasswordAsync(user, userInfo.Password);

        return passwordMatches;
    }

    public async Task<bool> GetOrCreateUserAsync(string provider, ExternalUserInfo userInfo)
    {
        var user = await _userManager.FindByLoginAsync(provider, userInfo.ProviderKey);

        var result = IdentityResult.Success;

        if (user is not null)
        {
            return result.Succeeded;
        }

        user = new ApplicationUser { UserName = userInfo.Username };

        result = await _userManager.CreateAsync(user);

        if (result.Succeeded)
        {
            result = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, userInfo.ProviderKey, displayName: null));
        }

        return result.Succeeded;
    }
}
