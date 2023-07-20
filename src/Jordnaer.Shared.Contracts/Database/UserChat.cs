namespace Jordnaer.Shared.Contracts;

public class UserChat
{
    public required string UserProfileId { get; set; }

    public required Guid ChatId { get; set; }
}
