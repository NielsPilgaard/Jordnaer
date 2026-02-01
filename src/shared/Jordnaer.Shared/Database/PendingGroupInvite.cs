namespace Jordnaer.Shared;

public class PendingGroupInvite
{
	public Guid Id { get; set; }
	public required Guid GroupId { get; set; }
	public Group Group { get; set; } = null!;

	/// <summary>
	/// The email address of the invited user (not yet registered).
	/// </summary>
	public required string Email { get; set; }

	/// <summary>
	/// Hashed unique token for verifying the invite.
	/// The unhashed token is sent via email.
	/// </summary>
	public required string TokenHash { get; set; }

	public PendingInviteStatus Status { get; set; } = PendingInviteStatus.Pending;

	public DateTime CreatedUtc { get; set; }
	public DateTime ExpiresAtUtc { get; set; }
	public DateTime? AcceptedAtUtc { get; set; }

	/// <summary>
	/// The user ID of the admin/owner who sent the invite.
	/// </summary>
	public string? InvitedByUserId { get; set; }
	public UserProfile? InvitedByUser { get; set; }
}
