using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Jordnaer.Shared;

public record GroupSearchFilter
{
	public string? Name { get; set; }
	public string[]? Categories { get; set; } = [];

	/// <summary>
	/// Only show group results within this many kilometers of the <see cref="Location"/>.
	/// </summary>
	[Range(1, 200, ErrorMessage = "Afstand skal være mellem 1 og 200 km")]
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
	public int PageSize { get; set; } = 11;

	[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
	public override int GetHashCode()
	{
		unchecked // Allow arithmetic overflow, numbers will just "wrap around"
		{
			var hash = 17;

			hash = hash * 23 + (Name?.GetHashCode() ?? 0);
			hash = hash * 23 + (Categories != null ? Categories.Aggregate(0, (current, category) => current + category.GetHashCode()) : 0);
			hash = hash * 23 + WithinRadiusKilometers.GetHashCode();
			hash = hash * 23 + (Location?.GetHashCode() ?? 0);
			hash = hash * 23 + Latitude.GetHashCode();
			hash = hash * 23 + Longitude.GetHashCode();

			return hash;
		}
	}

	public virtual bool Equals(GroupSearchFilter? other)
	{
		return other is not null &&
			   Name == other.Name &&
			   ((Categories == null && other.Categories == null) ||
				(Categories != null && other.Categories != null && Categories.SequenceEqual(other.Categories))) &&
			   WithinRadiusKilometers == other.WithinRadiusKilometers &&
			   Location == other.Location &&
			   Latitude == other.Latitude &&
			   Longitude == other.Longitude;
	}
}

file class RadiusRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var groupSearchFilter = (GroupSearchFilter)validationContext.ObjectInstance;

		// Check if location data exists
		var hasLocation = !string.IsNullOrEmpty(groupSearchFilter.Location) ||
						  (groupSearchFilter.Latitude.HasValue && groupSearchFilter.Longitude.HasValue);

		// Radius is only required when location is provided
		if (hasLocation && groupSearchFilter.WithinRadiusKilometers is null)
		{
			return new ValidationResult("Radius skal vælges når et område er valgt.");
		}

		return ValidationResult.Success!;
	}
}
