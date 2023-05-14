namespace Jordnaer.Shared.UserSearch;
public class UserSearchFilter
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }

    /// <summary>
    /// Only show user results within this many meters of the <see cref="ZipCode"/> or <see cref="Address"/>.
    /// </summary>
    public int? WithinRadiusMeters { get; set; }

    public List<string> LookingFor { get; set; } = new();
    public int? MinimumChildAge { get; set; }
    public int? MaximumChildAge { get; set; }
    public Gender? ChildGender { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
