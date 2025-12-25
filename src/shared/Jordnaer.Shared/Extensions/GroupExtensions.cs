namespace Jordnaer.Shared;

public static class GroupExtensions
{
    public static string DisplayLocation(this GroupSlim groupDto)
    {
        if (groupDto.ZipCode is not null && groupDto.City is not null)
        {
            return $"{groupDto.ZipCode}, {groupDto.City}";
        }

        if (groupDto.ZipCode is not null)
        {
            return groupDto.ZipCode.ToString()!;
        }

        return groupDto.City ?? "Ikke angivet";
    }
}
