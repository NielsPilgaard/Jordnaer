namespace Jordnaer.Shared;

public class UserSlim
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string ProfilePictureUrl { get; init; }

    public required string? UserName { get; init; }

    public override string ToString() => DisplayName;
}
