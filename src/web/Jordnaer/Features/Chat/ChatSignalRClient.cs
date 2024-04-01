using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Jordnaer.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Features.Chat;

public class ChatSignalRClient(
	CurrentUser currentUser,
	ILogger<AuthenticatedSignalRClientBase> logger,
	NavigationManager navigationManager)
	: AuthenticatedSignalRClientBase(logger, currentUser, navigationManager, "/hubs/chat")
{
	public void OnMessageReceived(Func<SendMessage, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(IChatHub.ReceiveChatMessage));
		HubConnection.On(nameof(IChatHub.ReceiveChatMessage), action);
	}

	public void OnChatStarted(Func<StartChat, Task> action)
	{
		if (HubConnection is null)
		{
			return;
		}

		HubConnection.Remove(nameof(IChatHub.StartChat));
		HubConnection.On(nameof(IChatHub.StartChat), action);
	}
}