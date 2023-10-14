using Jordnaer.Client.SignalR;
using Jordnaer.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Client.Features.Chat;

public class ChatSignalRClient : SignalRClientBase
{
    public ChatSignalRClient(NavigationManager navigationManager) : base(navigationManager, "/hubs/chat") { }

    public void OnMessageReceived(Func<ChatMessageDto, Task> action)
    {
        if (!Started && HubConnection is not null)
            HubConnection.On(nameof(IChatHub.ReceiveChatMessage), action);
    }

    public void OnChatStarted(Func<StartChat, Task> action)
    {
        if (!Started && HubConnection is not null)
            HubConnection.On(nameof(IChatHub.StartChat), action);
    }
}
