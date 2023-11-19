namespace Jordnaer.Client.SignalR;

public interface ISignalRClient : IAsyncDisposable
{
    bool IsConnected { get; }
    Task StartAsync();
}
