namespace Jordnaer.Shared;

public static class GroupDtoExtensions
{
    public static string DisplayLocation(this GroupDto userDto)
    {
        if (userDto.ZipCode is not null && userDto.City is not null)
        {
            return $"{userDto.ZipCode}, {userDto.City}";
        }

        if (userDto.ZipCode is not null)
        {
            return userDto.ZipCode.ToString()!;
        }

        return userDto.City ?? "Område ikke angivet";
    }
}
