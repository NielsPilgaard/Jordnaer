namespace Jordnaer.Shared;

public class GroupMembershipStatusChanged
{
	public required Guid GroupId { get; init; }
	public int PendingCountChange { get; init; }
}
