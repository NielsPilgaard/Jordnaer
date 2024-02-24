using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Jordnaer.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Features.Chat;

public class UnreadMessageSignalRClient(
	CurrentUser currentUser,
	ILogger<AuthenticatedSignalRClientBase> logger,
	IServer server,
	NavigationManager navigationManager)
	: AuthenticatedSignalRClientBase(currentUser, logger, server, navigationManager, "/hubs/chat")
{
	public void OnMessageReceived(Func<SendMessage, Task> action)
	{
		if (!Started && HubConnection is not null)
		{
			HubConnection.On(nameof(IChatHub.ReceiveChatMessage), action);
		}
	}

	public void OnChatStarted(Func<StartChat, Task> action)
	{
		if (!Started && HubConnection is not null)
		{
			HubConnection.On(nameof(IChatHub.StartChat), action);
		}
	}
}