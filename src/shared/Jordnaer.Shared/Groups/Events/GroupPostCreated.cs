namespace Jordnaer.Shared;

public class GroupPostCreated
{
	public required Guid PostId { get; init; }
	public required Guid GroupId { get; init; }
	public required string GroupName { get; init; }
	public required string AuthorId { get; init; }
	public required string AuthorDisplayName { get; init; }
	public required string PostText { get; init; }
	public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
