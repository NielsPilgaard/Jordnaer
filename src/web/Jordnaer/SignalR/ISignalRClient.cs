namespace Jordnaer.SignalR;

public interface ISignalRClient : IAsyncDisposable
{
	bool IsConnected { get; }
	Task StartAsync(CancellationToken cancellationToken = default);
}
