namespace Jordnaer.Shared.Contracts;

public class ChatUserDto
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string ProfilePictureUrl { get; init; }
}