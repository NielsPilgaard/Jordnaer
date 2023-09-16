namespace Jordnaer.Shared;

public static class UserProfileExtensions
{
    public static UserProfile Map(this UserProfile dto)
        => new()
        {
            Id = dto.Id,
            UserName = dto.UserName,
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

    public static ProfileDto ToProfileDto(this UserProfile userProfile)
        => new()
        {
            Id = userProfile.Id,
            FirstName = userProfile.FirstName,
            LastName = userProfile.LastName,
            Address = userProfile.Address,
            ChildProfiles = userProfile.ChildProfiles
                .Select(childProfile => childProfile.ToChildProfileDto())
                .ToList(),
            City = userProfile.City,
            DateOfBirth = userProfile.DateOfBirth,
            Description = userProfile.Description,
            LookingFor = userProfile.LookingFor,
            PhoneNumber = userProfile.PhoneNumber,
            ZipCode = userProfile.ZipCode,
            ProfilePictureUrl = userProfile.ProfilePictureUrl,
            CreatedUtc = userProfile.CreatedUtc,
            Age = userProfile.Age
        };

    public static UserSlim ToUserSlim(this UserProfile userProfile)
        => new()
        {
            DisplayName = $"{userProfile.FirstName} {userProfile.LastName}",
            Id = userProfile.Id,
            ProfilePictureUrl = userProfile.ProfilePictureUrl
        };
}
