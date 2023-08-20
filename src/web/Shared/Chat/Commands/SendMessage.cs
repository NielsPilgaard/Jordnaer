namespace Jordnaer.Shared;

public class SendMessage
{
    public required Guid Id { get; init; }

    public required Guid ChatId { get; init; }

    public required UserSlim Sender { get; init; }

    public required string Text { get; init; }

    public DateTime SentUtc { get; init; } = DateTime.UtcNow;

    public string? AttachmentUrl { get; init; }
}
