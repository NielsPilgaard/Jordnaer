using System.ComponentModel.DataAnnotations;

namespace Jordnaer.Shared;

public class DanishZipCodeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success!;
        }

        if (value is not int zipCode)
        {
            return new ValidationResult("Post nummer må kun bestå af tal.");
        }

        if (zipCode is < 1000 or > 10_000)
        {
            return new ValidationResult("Post nummer skal være mellem 1000 og 9999");
        }

        return ValidationResult.Success!;
    }
}
