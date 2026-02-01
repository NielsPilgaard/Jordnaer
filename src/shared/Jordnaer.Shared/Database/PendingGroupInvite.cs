using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Jordnaer.Shared;

[Index(nameof(Email), nameof(GroupId), IsUnique = true)]
[Index(nameof(TokenHash), IsUnique = true)]
public class PendingGroupInvite
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid Id { get; set; }

	[ForeignKey(nameof(Group))]
	public required Guid GroupId { get; set; }
	public Group Group { get; set; } = null!;

	/// <summary>
	/// The email address of the invited user (not yet registered).
	/// </summary>
	[Required]
	[EmailAddress]
	[MaxLength(256)]
	public required string Email { get; set; }

	/// <summary>
	/// Hashed unique token for verifying the invite.
	/// The unhashed token is sent via email.
	/// </summary>
	[Required]
	[MaxLength(128)]
	public required string TokenHash { get; set; }

	public PendingInviteStatus Status { get; set; } = PendingInviteStatus.Pending;

	public DateTime CreatedUtc { get; set; }
	public required DateTime ExpiresAtUtc { get; set; }
	public DateTime? AcceptedAtUtc { get; set; }

	/// <summary>
	/// The user ID of the admin/owner who sent the invite.
	/// </summary>
	[ForeignKey(nameof(InvitedByUser))]
	[MaxLength(450)]
	public string? InvitedByUserId { get; set; }
	public UserProfile? InvitedByUser { get; set; }
}
