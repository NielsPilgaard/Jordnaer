namespace Jordnaer.Shared.Contracts;

public class UserProfileLookingFor
{
    public required string UserProfileId { get; set; }

    public required int LookingForId { get; set; }
}
