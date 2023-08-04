namespace Jordnaer.Shared;

public static class LookingForExtensions
{
    public static void LoadValuesFrom(this LookingFor mapInto, LookingFor mapFrom)
    {
        mapInto.CreatedUtc = mapFrom.CreatedUtc;
        mapInto.Description = mapFrom.Description;
        mapInto.Name = mapFrom.Name;
        mapInto.Id = mapFrom.Id;
    }
}
