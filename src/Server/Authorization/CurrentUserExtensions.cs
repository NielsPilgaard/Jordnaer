using System.Security.Claims;
using Jordnaer.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Server.Authorization;

public static class CurrentUserExtensions
{
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddScoped<CurrentUser>();
        services.AddScoped<IClaimsTransformation, ClaimsTransformation>();
        return services;
    }

    private sealed class ClaimsTransformation : IClaimsTransformation
    {
        private readonly CurrentUser _currentUser;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimsTransformation(CurrentUser currentUser, UserManager<ApplicationUser> userManager)
        {
            _currentUser = currentUser;
            _userManager = userManager;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // We're not going to transform anything. We're using this as a hook into authorization
            // to set the current user without adding custom middleware.
            _currentUser.Principal = principal;

            string? loginProvider = principal.FindFirstValue("provider");

            if (principal.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } name)
            {
                // Resolve the user manager and see if the current user is a valid user in the database
                // we do this once and store it on the current user.
                _currentUser.User = loginProvider is null
                    ? await _userManager.FindByNameAsync(name)
                    : await _userManager.FindByLoginAsync(loginProvider, name);
            }

            return principal;
        }
    }
}
