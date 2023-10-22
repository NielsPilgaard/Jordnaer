namespace Jordnaer.Shared;

public static class ChildProfileExtensions
{
    public static void LoadValuesFrom(this ChildProfile mapInto, ChildProfile mapFrom)
    {
        mapInto.CreatedUtc = mapFrom.CreatedUtc;
        mapInto.Description = mapFrom.Description;
        mapInto.DateOfBirth = mapFrom.DateOfBirth;
        mapInto.FirstName = mapFrom.FirstName;
        mapInto.LastName = mapFrom.LastName;
        mapInto.Gender = mapFrom.Gender;
        mapInto.PictureUrl = mapFrom.PictureUrl;
        mapInto.Id = mapFrom.Id;
        mapInto.Age = mapFrom.Age;
    }

    public static ChildProfileDto ToChildProfileDto(this ChildProfile childProfile) =>
        new()
        {
            FirstName = childProfile.FirstName,
            LastName = childProfile.LastName,
            Gender = childProfile.Gender,
            PictureUrl = childProfile.PictureUrl,
            DateOfBirth = childProfile.DateOfBirth,
            Description = childProfile.Description,
            Age = childProfile.Age
        };
}
