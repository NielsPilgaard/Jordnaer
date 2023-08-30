namespace Jordnaer.Shared;

public interface IChatHub
{
    Task ReceiveChatMessage(ChatMessageDto message);
    Task StartChat(StartChat startChat);
}
