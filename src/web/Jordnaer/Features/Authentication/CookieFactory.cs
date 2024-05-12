using System.Net;

namespace Jordnaer.Features.Authentication;

public sealed class CookieFactory(ILogger<CookieFactory> logger)
{
	public Cookie Create(string name, string cookie, string domain)
	{
		logger.LogDebug("Creating cookie with domain {Domain}", domain);

		return new Cookie(name: name,
						  value: cookie,
						  path: "/",
						  domain: domain)
		{
			Secure = true,
			HttpOnly = true
		};
	}
}