using System.Security.Claims;

namespace RemindMeApp.Shared;
public readonly record struct ClaimDto(string Type, string Value);
public class CurrentUserDto
{
    public string? Name { get; set; }
    public string? AuthenticationSchema { get; set; }
    public bool IsAuthenticated { get; set; }
    public IEnumerable<ClaimDto>? Claims { get; set; }

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
            currentUser.IsAuthenticated = true;
        }

        currentUser.Claims = claimsPrincipal.Claims.Select(claim => new ClaimDto(claim.Type, claim.Value));
        return currentUser;
    }

    public ClaimsPrincipal ToClaimsPrincipal() =>
        IsAuthenticated is false || Claims is null
            ? new ClaimsPrincipal()
            : new ClaimsPrincipal(new ClaimsIdentity(claims: Claims.Select(claim => new Claim(claim.Type, claim.Value)), authenticationType: AuthenticationSchema));
}
