namespace Jordnaer.Shared;

public class GroupMembership
{
	public required Guid GroupId { get; set; }
	public required string UserProfileId { get; set; }

	public Group Group { get; set; } = null!;

	public UserProfile UserProfile { get; set; } = null!;

	/// <summary>
	/// Whether the user requested to join the group or was invited.
	/// </summary>
	public bool UserInitiatedMembership { get; set; }

	public DateTime CreatedUtc { get; set; }
	public DateTime LastUpdatedUtc { get; set; }

	public MembershipStatus MembershipStatus { get; set; }
	public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;
	public OwnershipLevel OwnershipLevel { get; set; } = OwnershipLevel.None;
}
