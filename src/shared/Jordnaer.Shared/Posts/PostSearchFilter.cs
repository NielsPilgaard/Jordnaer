﻿using System.ComponentModel.DataAnnotations;

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

	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}

file class RadiusRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var postSearchFilter = (PostSearchFilter)validationContext.ObjectInstance;

		if (postSearchFilter.WithinRadiusKilometers is null && string.IsNullOrEmpty(postSearchFilter.Location))
		{
			return ValidationResult.Success!;
		}

		return postSearchFilter.WithinRadiusKilometers is null
				   ? new ValidationResult("Radius skal vælges når et område er valgt.")
				   : ValidationResult.Success!;
	}
}

file class LocationRequiredAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var postSearchFilter = (PostSearchFilter)validationContext.ObjectInstance;

		if (postSearchFilter.WithinRadiusKilometers is null && string.IsNullOrEmpty(postSearchFilter.Location))
		{
			return ValidationResult.Success!;

		}

		return string.IsNullOrEmpty(postSearchFilter.Location)
				   ? new ValidationResult("Område skal vælges når en radius er valgt.")
				   : ValidationResult.Success!;
	}
}
