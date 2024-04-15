using System.Net;

namespace Jordnaer.Features.Authentication;

public sealed class CookieContainerFactory(ILogger<CookieContainerFactory> logger)
{
	public CookieContainer Create(string cookie, string domain)
	{
		var cookieContainer = new CookieContainer(1);

		cookieContainer.Add(CreateCookie(cookie, domain));

		return cookieContainer;
	}

	private Cookie CreateCookie(string cookie, string domain)
	{
		logger.LogDebug("Creating cookie with domain {Domain}", domain);

		return new Cookie(name: AuthenticationConstants.CookieName,
						  value: cookie,
						  path: "/",
						  domain: domain)
		{
			Secure = true,
			HttpOnly = true
		};
	}
}