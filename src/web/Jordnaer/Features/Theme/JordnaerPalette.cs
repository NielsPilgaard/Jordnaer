using MudBlazor;
using MudBlazor.Utilities;

namespace Jordnaer.Features.Theme;

public static class JordnaerTheme
{
	public static readonly MudTheme CustomTheme = new()
	{
		Typography = new Typography
		{
			Body1 = new Body1
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"],
				FontSize = "1.25rem"
			},
			Body2 = new Body2
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"],
				FontSize = "1.1rem"
			},
			Default = new Default
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"],
				FontSize = "1rem"
			}
		}
	};
}

public static class JordnaerPalette
{
	/// <summary>
	/// Yellow-orange. Used as background for texts and headers
	/// </summary>
	public static readonly MudColor YellowBackground = "#dbab45";

	/// <summary>
	/// Green. Used as background for texts and headers
	/// </summary>
	public static readonly MudColor GreenBackground = "#878e64";

	/// <summary>
	/// Dark Blue. Used for body text
	/// </summary>
	public static readonly MudColor BlueBody = "#41556b";

	/// <summary>
	/// Dark Red. Used for small texts, payoffs, quotes
	/// </summary>
	public static readonly MudColor RedHeader = "#673417";

	/// <summary>
	/// Beige. Used as background for text where <see cref="YellowBackground"/> and <see cref="GreenBackground"/> are too dark/saturated.
	/// </summary>
	public static readonly MudColor BeigeBackground = "#cfc1a699"; // 99 added to apply 60% opacity

	/// <summary>
	/// Pale Blue. Rarely used as background for text where <see cref="YellowBackground"/> and <see cref="GreenBackground"/> are too dark/saturated.
	/// </summary>
	public static readonly MudColor PaleBlueBackground = "#a9c0cf66"; // 66 added to apply 40% opacity
}
