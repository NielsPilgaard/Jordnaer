using Microsoft.AspNetCore.SignalR;

namespace Jordnaer.Server.Features.Chat;

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        //TODO: Add azure signalR
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}