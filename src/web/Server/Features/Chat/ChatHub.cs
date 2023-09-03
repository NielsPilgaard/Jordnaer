using Jordnaer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Server.Features.Chat;

[Authorize]
public class ChatHub : Hub<IChatHub>
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("User {userId} connected to {chatHub}", Context.User?.GetId(), nameof(ChatHub));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogError(exception, "User {userId} disconnected from {chatHub}. " +
                                        "Exception message: {exceptionMessage}",
                Context.User?.GetId(), nameof(ChatHub), exception.Message);
        }
        else
        {
            _logger.LogDebug("User {userId} disconnected from {chatHub}", Context.User?.GetId(), nameof(ChatHub));
        }

        await base.OnConnectedAsync();
    }

    public async Task SendChatMessageAsync(ChatMessageDto chatMessage, string userId)
        => await Clients.User(userId).ReceiveChatMessage(chatMessage);

    public async Task StartChatAsync(StartChat startChat)
        => await Clients
            .Users(startChat
                .Recipients
                .Select(user => user.Id))
            .StartChat(startChat);
}
