using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.SignalR;

public abstract class SignalRClientBase : ISignalRClient
{
	protected bool Started { get; private set; }

	protected SignalRClientBase(NavigationManager navigationManager, string hubPath)
	{
		HubConnection = new HubConnectionBuilder()
			.WithUrl(navigationManager.ToAbsoluteUri(hubPath))
			.WithAutomaticReconnect()
			.Build();
	}

	public bool IsConnected => HubConnection?.State is HubConnectionState.Connected;

	protected HubConnection? HubConnection { get; }

	public async ValueTask DisposeAsync()
	{
		if (HubConnection is not null)
		{
			await HubConnection.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		if (!Started && HubConnection is not null)
		{
			await HubConnection.StartAsync(cancellationToken);
			Started = true;
		}
	}
}
