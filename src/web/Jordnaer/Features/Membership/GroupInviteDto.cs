using Jordnaer.Shared;

namespace Jordnaer.Features.Membership;

public class GroupInviteDto
{
	public required Guid GroupId { get; set; }
	public required string GroupName { get; set; }
	public string? GroupDescription { get; set; }
	public string? GroupProfilePictureUrl { get; set; }
	public required string InvitedByUserName { get; set; }
	public required DateTime InvitedAtUtc { get; set; }
}
