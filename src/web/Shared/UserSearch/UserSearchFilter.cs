namespace Jordnaer.Shared.UserSearch;
public class UserSearchFilter
{
    public string? Name { get; set; }
    public List<string>? LookingFor { get; set; }

    /// <summary>
    /// Only show user results within this many meters of the <see cref="Location"/>.
    /// </summary>
    public int? WithinRadiusMeters { get; set; }
    public string? Location { get; set; }

    public int? MinimumChildAge { get; set; }
    public int? MaximumChildAge { get; set; }
    public Gender? ChildGender { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
