using MudBlazor;
using MudBlazor.Utilities;

namespace Jordnaer.Features.Theme;

public static class JordnaerTheme
{
	public static readonly MudTheme CustomTheme = new()
	{
		Typography = new Typography
		{
			Body1 = new Body1Typography
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"],
				FontSize = "1.25rem"
			},
			Body2 = new Body2Typography
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"],
				FontSize = "1.1rem"
			},
			Default = new DefaultTypography
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"],
				FontSize = "1rem"
			}
		},
		PaletteLight = new PaletteLight
		{
			Primary = "#dbab45",        // GLÆDE - warm yellow-orange for primary actions
			Secondary = "#878e64",      // RO - calm green for secondary actions
			Tertiary = "#a9c0cf",       // LEG - playful light blue
			Info = "#41556b",           // MØDE - informative blue
			Success = "#878e64",        // RO - green is perfect for success
			Warning = "#dbab45",        // GLÆDE - yellow-orange for warnings
			Error = "#673417",          // MØDE Red - red-brown for errors
			Dark = "#41556b",           // MØDE - body text blue
			TextPrimary = "#41556b",    // MØDE - main text color
			TextSecondary = "#673417",  // MØDE Red - secondary text
			Background = "#FFFFFF",     // Pure white
			Surface = "#FFFFFF",        // Pure white for better contrast with text
			AppbarBackground = "#dbab45", // GLÆDE - warm yellow header

			// Text on colored buttons
			PrimaryContrastText = "#FFFFFF",
			SecondaryContrastText = "#FFFFFF",
			TertiaryContrastText = "#41556b",  // Dark text on light blue
			InfoContrastText = "#FFFFFF",
			SuccessContrastText = "#FFFFFF",
			WarningContrastText = "#FFFFFF",
			ErrorContrastText = "#FFFFFF"
		}
	};
}

public static class JordnaerPalette
{
	/// <summary>
	/// Yellow-orange. Used as background for texts and headers (GLÆDE)
	/// </summary>
	public static readonly MudColor YellowBackground = "#dbab45";

	/// <summary>
	/// Green. Used as background for texts and headers (RO)
	/// </summary>
	public static readonly MudColor GreenBackground = "#878e64";

	/// <summary>
	/// Dark Blue. Used for body text (MØDE)
	/// </summary>
	public static readonly MudColor BlueBody = "#41556b";

	/// <summary>
	/// Dark Red. Used for small texts, payoffs, quotes (MØDE Red)
	/// </summary>
	public static readonly MudColor RedHeader = "#673417";

	/// <summary>
	/// Beige. Used as background for text where <see cref="YellowBackground"/> and <see cref="GreenBackground"/> are too dark/saturated. (OMSORG)
	/// </summary>
	public static readonly MudColor BeigeBackground = "#cfc1a6";

	/// <summary>
	/// Beige at 60% opacity.
	/// </summary>
	public static readonly MudColor BeigeBackground60 = "#cfc1a699";

	/// <summary>
	/// Pale Blue. Rarely used as background for text where <see cref="YellowBackground"/> and <see cref="GreenBackground"/> are too dark/saturated. (LEG)
	/// </summary>
	public static readonly MudColor PaleBlueBackground = "#a9c0cf";

	/// <summary>
	/// Pale Blue at 40% opacity.
	/// </summary>
	public static readonly MudColor PaleBlueBackground40 = "#a9c0cf66";

	/// <summary>
	/// Warm White. Used as a soft alternative to white for long-form text (OMSORG)
	/// </summary>
	public static readonly MudColor WarmWhite = "#F0EBE3";

	/// <summary>
	/// Light Blue. A softer version of <see cref="PaleBlueBackground"/> for long-form text.
	/// </summary>
	public static readonly MudColor LightBlue = "#E6F0F7";

	/// <summary>
	/// Pure White. Used for text on dark backgrounds and button backgrounds.
	/// </summary>
	public static readonly MudColor White = "#FFFFFF";
}
