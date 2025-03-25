using Jordnaer.Features.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.SignalR;

public abstract class AuthenticatedSignalRClientBase : ISignalRClient
{
	private readonly ILogger<AuthenticatedSignalRClientBase> _logger;

	protected AuthenticatedSignalRClientBase(
		ILogger<AuthenticatedSignalRClientBase> logger,
		CurrentUser currentUser,
		NavigationManager navigationManager,
		string hubPath)
	{
		_logger = logger;

		if (currentUser.CookieContainer is null)
		{
			_logger.LogInformation("Cannot create authenticated HubConnection, " +
								   "CurrentUser {UserId} does not have a cookie.", currentUser.Id);
			return;
		}

		HubConnection = new HubConnectionBuilder()
						.WithUrl(navigationManager.ToAbsoluteUri(hubPath),
								 options => options.Cookies = currentUser.CookieContainer)
						.WithAutomaticReconnect()
						.Build();
	}

	protected bool Started { get; private set; }

	public bool IsConnected => HubConnection?.State is HubConnectionState.Connected;

	protected HubConnection? HubConnection { get; }

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		if (!Started && HubConnection?.State is HubConnectionState.Disconnected)
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

	public async ValueTask DisposeAsync()
	{
		if (HubConnection is not null)
		{
			_logger.LogDebug("Disposing SignalR Client");

			await HubConnection.DisposeAsync();

			_logger.LogDebug("SignalR Client disposed");
		}

		GC.SuppressFinalize(this);
	}
}