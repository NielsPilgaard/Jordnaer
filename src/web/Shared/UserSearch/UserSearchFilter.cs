using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared.UserSearch;
public class UserSearchFilter
{
    public string? Name { get; set; }
    public string[]? LookingFor { get; set; }

    /// <summary>
    /// Only show user results within this many kilometers of the <see cref="Location"/>.
    /// </summary>
    [Range(1, 50, ErrorMessage = "Afstand skal være mellem 1 og 50km")]
    public int? WithinRadiusKilometers { get; set; }
    public string? Location { get; set; }

    [Range(0, 18, ErrorMessage = "Skal være mellem 0 og 18 år")]
    public int? MinimumChildAge { get; set; }
    [Range(0, 18, ErrorMessage = "Skal være mellem 0 og 18 år")]
    public int? MaximumChildAge { get; set; }
    public Gender? ChildGender { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
