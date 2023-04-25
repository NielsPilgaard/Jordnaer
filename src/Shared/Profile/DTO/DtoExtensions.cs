namespace Jordnaer.Shared;

public static class DtoExtensions
{
    public static UserProfile Map(this UserProfileDto dto)
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
            Interests = dto.Interests,
            LookingFor = dto.LookingFor,
            PhoneNumber = dto.PhoneNumber,
            ZipCode = dto.ZipCode,
            ProfilePictureUrl = dto.ProfilePictureUrl
        };
}
