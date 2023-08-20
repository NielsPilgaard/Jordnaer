namespace Jordnaer.Shared;

public class SetChatName
{
    public required Guid ChatId { get; init; }
    public required string Name { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
