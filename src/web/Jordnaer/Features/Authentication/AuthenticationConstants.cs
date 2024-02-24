using Microsoft.AspNetCore.Identity;

namespace Jordnaer.Features.Authentication;

public static class AuthenticationConstants
{
	public static readonly string CookieName = $".AspNetCore.{IdentityConstants.ApplicationScheme}";
}