using Jordnaer.Features.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.SignalR;

public abstract class AuthenticatedSignalRClientBase : ISignalRClient
{
	private readonly ILogger<AuthenticatedSignalRClientBase> _logger;

	protected AuthenticatedSignalRClientBase(
		ILogger<AuthenticatedSignalRClientBase> logger,
		CookieContainerFactory cookieContainerFactory,
		NavigationManager navigationManager,
		string hubPath)
	{
		_logger = logger;

		var cookieContainer = cookieContainerFactory.Create();
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
		if (HubConnection?.State is HubConnectionState.Connected)
		{
			_logger.LogDebug("Stopping SignalR Client");
			await HubConnection.StopAsync(cancellationToken);
			_logger.LogDebug("SignalR Client stopped");
		}
		else
		{
			_logger.LogDebug("Stop SignalR was called, but the Connection is currently in the {State} state. " +
							 "No further action is taken.", HubConnection?.State);
		}
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