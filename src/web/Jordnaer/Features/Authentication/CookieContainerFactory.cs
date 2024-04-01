using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Jordnaer.Features.Authentication;

public sealed class CookieContainerFactory(
	ILogger<CookieContainerFactory> logger,
	IServer server,
	CurrentUser currentUser)
{
	public CookieContainer? Create()
	{
		if (currentUser.Id is null)
		{
			logger.LogDebug("CurrentUser is not logged in, cannot create an authenticated SignalR Connection.");
			return null;
		}

		if (currentUser.Cookie is null)
		{
			logger.LogWarning("CurrentUser {UserId} does not have a cookie, cannot create an authenticated SignalR Connection.", currentUser.Id);
			return null;
		}

		var allServerUris = server.Features.Get<IServerAddressesFeature>()?.Addresses;
		logger.LogDebug("All server addresses: {@ServerAddresses}", allServerUris);

		var serverUri = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
		if (serverUri is null)
		{
			logger.LogError("Failed to get server address from IServer");
			return null;
		}

		var cookieContainer = new CookieContainer(1);

		if (!serverUri.Contains("[::]"))
		{
			cookieContainer.Add(CreateCookie(new Uri(serverUri).Host));
			return cookieContainer;
		}

		logger.LogInformation(
			"First server address was {ServerUri}, trying to get hostname from environment variable 'WEBSITE_HOSTNAME' instead.", serverUri);

		const string domain = "mini-moeder.dk";

		cookieContainer.Add(CreateCookie(domain));

		return cookieContainer;
	}

	private Cookie CreateCookie(string domain)
	{
		logger.LogInformation("Creating cookie with domain {Domain}", domain);

		return new Cookie(name: AuthenticationConstants.CookieName,
						  value: currentUser.Cookie,
						  path: "/",
						  domain: domain)
		{
			Secure = true,
			HttpOnly = true
		};
	}
}