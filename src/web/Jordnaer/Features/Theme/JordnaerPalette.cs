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
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"]
			},
			Body2 = new Body2
			{
				FontFamily = ["Open Sans Light", "Arial", "sans-serif"]
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

	public static readonly string BlueBodyTextStyle = $"color: {BlueBody}";

	/// <summary>
	/// Dark Red. Used for small texts, payoffs, quotes
	/// </summary>
	public static readonly MudColor RedHeader = "#673417";

	public static readonly string RedHeaderTextStyle = $"color: {RedHeader}";

	/// <summary>
	/// Beige. Used as background for text where <see cref="YellowBackground"/> and <see cref="GreenBackground"/> are too dark/saturated.
	/// </summary>
	public static readonly MudColor BeigeBackground = "#dbd2c1"; // Changed from "#cfc1a6" to "#dbd2c1"

	/// <summary>
	/// Pale Blue. Rarely used as background for text where <see cref="YellowBackground"/> and <see cref="GreenBackground"/> are too dark/saturated.
	/// </summary>
	public static readonly MudColor PaleBlueBackground = "#a9c0cf";
}
