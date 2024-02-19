using Jordnaer.Database;
using Jordnaer.Features.Authentication;
using Jordnaer.Shared;
using Jordnaer.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jordnaer.Features.Chat;

public class ChatSignalRClient(
	CurrentUser currentUser,
	ILogger<AuthenticatedSignalRClientBase> logger,
	UserManager<ApplicationUser> userManager,
	IServer server,
	NavigationManager navigationManager)
	: AuthenticatedSignalRClientBase(currentUser, logger, userManager, server, navigationManager, "/hubs/chat")
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
