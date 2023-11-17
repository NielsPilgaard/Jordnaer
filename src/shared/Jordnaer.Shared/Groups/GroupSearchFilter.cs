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

file class RadiusRequiredAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var userSearchFilter = (GroupSearchFilter)validationContext.ObjectInstance;

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
        var userSearchFilter = (GroupSearchFilter)validationContext.ObjectInstance;

        if (userSearchFilter.WithinRadiusKilometers is null && string.IsNullOrEmpty(userSearchFilter.Location))
        {
            return ValidationResult.Success!;

        }

        return string.IsNullOrEmpty(userSearchFilter.Location)
            ? new ValidationResult("Område skal vælges når en radius er valgt.")
            : ValidationResult.Success!;
    }
}
