namespace Jordnaer.Shared;

public static class UserProfileExtensions
{
    public static UserProfile Map(this UserProfile dto)
        => new()
        {
            Id = dto.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Address = dto.Address,
            ChildProfiles = dto.ChildProfiles,
            City = dto.City,
            DateOfBirth = dto.DateOfBirth,
            Description = dto.Description,
            LookingFor = dto.LookingFor,
            PhoneNumber = dto.PhoneNumber,
            ZipCode = dto.ZipCode,
            ProfilePictureUrl = dto.ProfilePictureUrl,
            Contacts = dto.Contacts,
            CreatedUtc = dto.CreatedUtc
        };
}
