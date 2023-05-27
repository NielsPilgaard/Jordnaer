using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared.UserSearch;

public class DataForsyningenOptions
{
    public const string SectionName = "DataForsyningen";

    [Url]
    [Required]
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed search radius, in meters.
    /// </summary>
    public int MaxSearchRadiusKilometers { get; set; } = 50;
}
