using System.Security.Claims;

namespace Jordnaer.Features.Authentication;

public class UserService
{
	public CurrentUser CurrentUser = new();

	internal void SetUser(ClaimsPrincipal user)
	{
		CurrentUser.User = user;
	}

	internal void SetCookie(string cookie)
	{
		CurrentUser.Cookie = cookie;
	}
}