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
		//TODO: SignalR is flaky on Azure, maybe locally, investigate
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