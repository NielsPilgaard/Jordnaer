namespace Jordnaer.Shared;

public interface IChatHub
{
    Task ReceiveChatMessage(SendMessage message);
    Task StartChat(StartChat startChat);
}
