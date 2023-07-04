using Jordnaer.Server.Authentication;
using Jordnaer.Server.Database;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Server.Features.DeleteUser;

public interface IDeleteUserService
{
    Task<bool> DeleteUserAsync(string id);
}

public class DeleteUserService : IDeleteUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserService> _logger;

    public DeleteUserService(UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    // TODO: Perform all cleanup to delete a user completely
    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            _logger.LogWarning("Failed to delete user with id {userId}. No such user exists.", id);
            return false;
        }
        var identityResult = await _userManager.DeleteAsync(user);
        if (identityResult.Succeeded is false)
        {
            _logger.LogError("Failed to delete user {userEmail}. Errors: {@identityResultErrors}",
                user.Email, identityResult.Errors.Select(error => $"ErrorCode {error.Code}: {error.Description}"));
        }

        return identityResult.Succeeded;
    }
}
