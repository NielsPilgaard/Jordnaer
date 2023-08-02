namespace Jordnaer.Shared.Contracts;

public static class MessagingConstants
{
    public static readonly string[] QueueNames = { StartChat, SendMessage, SetChatName };

    public const string StartChat = "start-chat";
    public const string SendMessage = "send-message";
    public const string SetChatName = "set-chat-name";
}
