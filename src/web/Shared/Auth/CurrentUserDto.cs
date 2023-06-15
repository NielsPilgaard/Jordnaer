using System.Security.Claims;

namespace Jordnaer.Shared.Auth;
public readonly record struct ClaimDto(string Type, string Value);

public class CurrentUserDto
{
    public string? Name { get; set; }
    public string? AuthenticationSchema { get; set; }
    public IEnumerable<ClaimDto> Claims { get; set; } = Enumerable.Empty<ClaimDto>();

    public static CurrentUserDto From(ClaimsPrincipal? claimsPrincipal)
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

        currentUser.Claims = claimsPrincipal.Claims.Select(claim => new ClaimDto(claim.Type, claim.Value));
        return currentUser;
    }

    public ClaimsPrincipal ToClaimsPrincipal() =>
        Claims.Any() is false
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(new ClaimsIdentity(claims: Claims.Select(claim => new Claim(claim.Type, claim.Value)), authenticationType: AuthenticationSchema));
}
