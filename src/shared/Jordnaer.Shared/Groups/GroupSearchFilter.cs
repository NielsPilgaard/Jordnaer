using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class GroupSearchFilter
{
    public string? Name { get; set; }
    public string[]? Categories { get; set; }

    /// <summary>
    /// Only show group results within this many kilometers of the <see cref="Location"/>.
    /// </summary>
    [Range(1, 50, ErrorMessage = "Afstand skal være mellem 1 og 50 km")]
    [LocationRequired]
    public int? WithinRadiusKilometers { get; set; } = 5;

    [RadiusRequired]
    public string? Location { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
