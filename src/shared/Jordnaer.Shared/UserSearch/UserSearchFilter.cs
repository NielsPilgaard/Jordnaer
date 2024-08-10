using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Jordnaer.Shared;
public record UserSearchFilter
{
	public string? Name { get; set; }
	public string[]? Categories { get; set; } = [];

	/// <summary>
	/// Only show user results within this many kilometers of the <see cref="Location"/>.
	/// </summary>
	[Range(1, 50, ErrorMessage = "Afstand skal være mellem 1 og 50 km")]
	[LocationRequired]
	public int? WithinRadiusKilometers { get; set; }

	[RadiusRequired]
	public string? Location { get; set; }

	[Range(0, 18, ErrorMessage = "Skal være mellem 0 og 18 år")]
	public int? MinimumChildAge { get; set; }
	[Range(0, 18, ErrorMessage = "Skal være mellem 0 og 18 år")]
	public int? MaximumChildAge { get; set; }
	public Gender? ChildGender { get; set; }

	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 10;

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
			hash = hash * 23 + MinimumChildAge.GetHashCode();
			hash = hash * 23 + MaximumChildAge.GetHashCode();
			hash = hash * 23 + ChildGender.GetHashCode();

			return hash;
		}
	}

	public virtual bool Equals(UserSearchFilter? other)
	{
		return other is not null &&
			   Name == other.Name &&
			   ((Categories == null && other.Categories == null) ||
				(Categories != null && other.Categories != null && Categories.SequenceEqual(other.Categories))) &&
			   WithinRadiusKilometers == other.WithinRadiusKilometers &&
			   Location == other.Location &&
			   MinimumChildAge == other.MinimumChildAge &&
			   MaximumChildAge == other.MaximumChildAge &&
			   ChildGender == other.ChildGender;
	}
}

file class RadiusRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var userSearchFilter = (UserSearchFilter)validationContext.ObjectInstance;

		if (userSearchFilter.WithinRadiusKilometers is null && string.IsNullOrEmpty(userSearchFilter.Location))
		{
			return ValidationResult.Success!;
		}

		return userSearchFilter.WithinRadiusKilometers is null
			? new ValidationResult("Radius skal vælges når et område er valgt.")
			: ValidationResult.Success!;
	}
}
file class LocationRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var userSearchFilter = (UserSearchFilter)validationContext.ObjectInstance;

		if (userSearchFilter.WithinRadiusKilometers is null && string.IsNullOrEmpty(userSearchFilter.Location))
		{
			return ValidationResult.Success!;

		}

		return string.IsNullOrEmpty(userSearchFilter.Location)
			? new ValidationResult("Område skal vælges når en radius er valgt.")
			: ValidationResult.Success!;
	}
}
