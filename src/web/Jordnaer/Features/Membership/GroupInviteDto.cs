namespace Jordnaer.Features.Membership;

public record GroupInviteDto
{
	public required Guid GroupId { get; init; }
	public required string GroupName { get; init; }
	public string? GroupDescription { get; init; }
	public string? GroupProfilePictureUrl { get; init; }
	public required string InvitedByUserName { get; init; }
	public required DateTime InvitedAtUtc { get; init; }
}
