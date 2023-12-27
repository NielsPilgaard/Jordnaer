namespace Jordnaer.Shared;

public class UserGroupAccess
{
    public required GroupSlim Group { get; init; }
    public required DateTime LastUpdatedUtc { get; init; }
    public required PermissionLevel PermissionLevel { get; init; }
    public required MembershipStatus MembershipStatus { get; init; }
    public required OwnershipLevel OwnershipLevel { get; init; }
}
