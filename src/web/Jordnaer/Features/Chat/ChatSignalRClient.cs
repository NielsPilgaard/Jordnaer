using Jordnaer.Shared;
using Jordnaer.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Features.Chat;

public class ChatSignalRClient : SignalRClientBase
{
	public ChatSignalRClient(NavigationManager navigationManager) : base(navigationManager, "/hubs/chat") { }

	public void OnMessageReceived(Func<SendMessage, Task> action)
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
