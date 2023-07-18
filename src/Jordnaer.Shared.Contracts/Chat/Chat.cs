namespace Jordnaer.Shared.Contracts.Chat;

public class Chat
{
    public required string Recipient { get; set; }
    public required List<ChatMessage> Messages { get; set; } = new();
}

