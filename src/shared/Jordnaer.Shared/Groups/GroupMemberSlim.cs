namespace Jordnaer.Shared;

public class GroupMemberSlim
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string ProfilePictureUrl { get; init; }

    public required string? UserName { get; init; }

    public required OwnershipLevel OwnershipLevel { get; init; }

    public required PermissionLevel PermissionLevel { get; init; }

    public override string ToString() => DisplayName;
}
