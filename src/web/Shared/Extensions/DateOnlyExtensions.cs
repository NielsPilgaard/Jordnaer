namespace Jordnaer.Shared;

public static class DateOnlyExtensions
{
    public static int GetAge(this DateTime dateOfBirth)
    {
        int age = DateTime.Now.Year - dateOfBirth.Year;

        if (DateTime.Now.Month < dateOfBirth.Month ||
            DateTime.Now.Month == dateOfBirth.Month && DateTime.Now.Day < dateOfBirth.Day)
        {
            age--;
        }

        return age;
    }

    public static int? GetAge(this DateTime? dateOfBirth) => dateOfBirth?.GetAge();
}
