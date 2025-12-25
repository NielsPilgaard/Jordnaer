namespace Jordnaer.Shared;

public static class UserDtoExtensions
{
    public static string DisplayLocation(this UserDto userDto)
    {
        if (userDto.ZipCode is not null && userDto.City is not null)
        {
            return $"{userDto.ZipCode}, {userDto.City}";
        }

        if (userDto.ZipCode is not null)
        {
            return userDto.ZipCode.ToString()!;
        }

        return userDto.City ?? "Ikke angivet";
    }
}
