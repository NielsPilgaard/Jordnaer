using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Jordnaer.Shared.Validation;

/// <summary>
/// Validates that a string value is a valid hex color in #RGB or #RRGGBB format.
/// </summary>
public partial class HexColorAttribute : ValidationAttribute
{
	public HexColorAttribute() : base("The field {0} must be a valid hex color (e.g., #FFF or #FFFFFF).")
	{
	}

	public override bool IsValid(object? value)
	{
		if (value is null)
		{
			return true; // Null values are handled by [Required] if needed
		}

		if (value is not string hexColor)
		{
			return false;
		}

		return HexColorRegex().IsMatch(hexColor);
	}

	[GeneratedRegex(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$")]
	private static partial Regex HexColorRegex();
}
