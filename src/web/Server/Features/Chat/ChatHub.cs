using Jordnaer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Server.Features.Chat;

[Authorize]
public class ChatHub : Hub<IChatHub>
{
    public async Task SendChatMessageAsync(ChatMessageDto chatMessage, string userId)
    {
        await Clients.User(userId).ReceiveChatMessage(chatMessage);
    }
    public async Task StartChatAsync(StartChat startChat)
    {
        await Clients
            .Users(startChat
                .Recipients
                .Select(user => user.Id))
            .StartChat(startChat);
    }
}
