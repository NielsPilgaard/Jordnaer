using System.Security.Claims;

namespace Jordnaer.Shared;

public static class ClaimsPrincipalExtensions
{
    public static string GetId(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.NameIdentifier)!.Value;
}
