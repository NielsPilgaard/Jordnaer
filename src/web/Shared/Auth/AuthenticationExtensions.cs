using System.Security.Claims;

namespace Jordnaer.Shared;

public static class AuthenticationExtensions
{
    public static ClaimsPrincipal ToClaimsPrincipal(this CurrentUserDto user) =>
        user.Claims.Any() is false
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(new ClaimsIdentity(
                claims: user.Claims.Select(claim => new System.Security.Claims.Claim(claim.Type, claim.Value)),
                authenticationType: user.AuthenticationSchema));

    public static CurrentUserDto ToCurrentUser(this ClaimsPrincipal claimsPrincipal)
    {
        var currentUser = new CurrentUserDto();
        if (claimsPrincipal?.Identity is null)
        {
            return currentUser;
        }

        if (claimsPrincipal.Identity.IsAuthenticated)
        {
            currentUser.AuthenticationSchema = claimsPrincipal.Identity.AuthenticationType;
        }

        currentUser.Claims = claimsPrincipal.Claims.Select(claim => new Claim(claim.Type, claim.Value));
        return currentUser;
    }
}
