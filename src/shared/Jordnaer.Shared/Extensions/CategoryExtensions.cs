namespace Jordnaer.Shared;

public static class CategoryExtensions
{
    public static void LoadValuesFrom(this Category mapInto, Category mapFrom)
    {
        mapInto.CreatedUtc = mapFrom.CreatedUtc;
        mapInto.Description = mapFrom.Description;
        mapInto.Name = mapFrom.Name;
        mapInto.Id = mapFrom.Id;
    }
}
