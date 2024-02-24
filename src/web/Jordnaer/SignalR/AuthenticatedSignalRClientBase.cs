using Jordnaer.Features.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;

namespace Jordnaer.SignalR;

public abstract class AuthenticatedSignalRClientBase : ISignalRClient
{
	private readonly ILogger<AuthenticatedSignalRClientBase> _logger;
	private readonly IServer _server;

	protected AuthenticatedSignalRClientBase(CurrentUser currentUser,
		ILogger<AuthenticatedSignalRClientBase> logger,
		IServer server,
		NavigationManager navigationManager,
		string hubPath)
	{
		_logger = logger;
		_server = server;

		if (currentUser.Id is null)
		{
			logger.LogDebug("CurrentUser is not logged in, cannot create an authenticated SignalR Connection.");
			return;
		}

		if (currentUser.Cookie is null)
		{
			logger.LogWarning("CurrentUser {UserId} does not have a cookie, cannot create an authenticated SignalR Connection.", currentUser.Id);
			return;
		}

		var cookieContainer = CreateCookieContainer(currentUser.Cookie);
		if (cookieContainer is null)
		{
			return;
		}

		HubConnection = new HubConnectionBuilder()
						.WithUrl(navigationManager.ToAbsoluteUri(hubPath),
								 options => options.Cookies = cookieContainer)
						.WithAutomaticReconnect()
						.Build();
	}

	protected bool Started { get; private set; }

	public bool IsConnected => HubConnection?.State is HubConnectionState.Connected;

	protected HubConnection? HubConnection { get; }

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		if (!Started && HubConnection is not null)
		{
			_logger.LogDebug("Starting SignalR Client");
			await HubConnection.StartAsync(cancellationToken);
			Started = true;
			_logger.LogDebug("SignalR Client Started");
		}
	}

	public async Task StopAsync(CancellationToken cancellationToken = default)
	{
		if (Started && HubConnection is not null)
		{
			_logger.LogDebug("Stopping SignalR Client");
			await HubConnection.StopAsync(cancellationToken);
			Started = false;
			_logger.LogDebug("SignalR Client stopped");
		}
	}

	private CookieContainer? CreateCookieContainer(string cookie)
	{
		var allServerUris = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
		_logger.LogDebug("All server addresses: {@ServerAddresses}", allServerUris);

		var serverUri = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
		if (serverUri is null)
		{
			_logger.LogError("Failed to get server address from IServer");
			return null;
		}

		if (!serverUri.Contains("[::]"))
		{
			var cookieContainer = new CookieContainer(1);
			cookieContainer.Add(new Cookie(
									name: AuthenticationConstants.CookieName,
									value: cookie,
									path: "/",
									domain: new Uri(serverUri).Host));
			return cookieContainer;
		}

		_logger.LogInformation(
			"First server address was {ServerUri}, trying to get hostname from environment variable 'WEBSITE_HOSTNAME' instead.", serverUri);

		var domain = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
		if (domain is null)
		{
			_logger.LogError("Cannot determine domain for Cookie, " +
							 "environment variable 'WEBSITE_HOSTNAME' was null.");

			return null;
		}

		var container = new CookieContainer(1);
		container.Add(new Cookie(name: AuthenticationConstants.CookieName,
								 value: cookie,
								 path: "/",
								 domain: domain));
		return container;
	}

	public async ValueTask DisposeAsync()
	{
		if (HubConnection is not null)
		{
			_logger.LogInformation("Disposing SignalR Client");
			await HubConnection.DisposeAsync();
			_logger.LogInformation("SignalR Client disposed");
		}

		GC.SuppressFinalize(this);
	}
}