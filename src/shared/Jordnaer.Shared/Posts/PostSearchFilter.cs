using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class PostSearchFilter
{
	public string? Contents { get; set; }
	public string[]? Categories { get; set; } = [];

	/// <summary>
	/// Only show user results within this many kilometers of the <see cref="Location"/>.
	/// </summary>
	[Range(1, 50, ErrorMessage = "Afstand skal være mellem 1 og 50 km")]
	[LocationRequired]
	public int? WithinRadiusKilometers { get; set; }

	[RadiusRequired]
	public string? Location { get; set; }

	/// <summary>
	/// Latitude coordinate for map-based location search.
	/// When set (along with Longitude), takes precedence over Location string.
	/// </summary>
	public double? Latitude { get; set; }

	/// <summary>
	/// Longitude coordinate for map-based location search.
	/// When set (along with Latitude), takes precedence over Location string.
	/// </summary>
	public double? Longitude { get; set; }

	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}

file class RadiusRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var postSearchFilter = (PostSearchFilter)validationContext.ObjectInstance;

		// Check if location data exists
		var hasLocation = !string.IsNullOrEmpty(postSearchFilter.Location) ||
						  (postSearchFilter.Latitude.HasValue && postSearchFilter.Longitude.HasValue);

		// Radius is only required when location is provided
		if (hasLocation && postSearchFilter.WithinRadiusKilometers is null)
		{
			return new ValidationResult("Radius skal vælges når et område er valgt.");
		}

		return ValidationResult.Success!;
	}
}

file class LocationRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var postSearchFilter = (PostSearchFilter)validationContext.ObjectInstance;

		// Check if location data exists
		var hasLocation = !string.IsNullOrEmpty(postSearchFilter.Location) ||
						  (postSearchFilter.Latitude.HasValue && postSearchFilter.Longitude.HasValue);

		// If no location is provided, search is valid (radius will be ignored)
		// This allows searching without location even if radius slider has a value
		if (!hasLocation)
		{
			return ValidationResult.Success!;
		}

		return ValidationResult.Success!;
	}
}
