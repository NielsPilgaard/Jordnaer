using MudBlazor.Utilities;

namespace Jordnaer.Client.Features.Theme;

public static class JordnaerPalette
{
    /// <summary>
    /// Yellow-orange. Used as background for texts and headers
    /// </summary>
    public static readonly MudColor Primary = "#dbab45";

    /// <summary>
    /// Green. Used as background for texts and headers
    /// </summary>
    public static readonly MudColor Secondary = "#878e64";

    /// <summary>
    /// Dark Blue. Used for body text
    /// </summary>
    public static readonly MudColor Body = "#41556b";

    /// <summary>
    /// Dark Red. Used for small texts, payoffs, quotes
    /// </summary>
    public static readonly MudColor Header = "#673417";

    /// <summary>
    /// Beige. Used as background for text where <see cref="Primary"/> and <see cref="Secondary"/> are too dark/saturated.
    /// </summary>
    public static readonly MudColor DimBackground = "#dbd2c1"; // Changed from "#cfc1a6" to "#dbd2c1"

    /// <summary>
    /// Pale Blue. Rarely used as background for text where <see cref="Primary"/> and <see cref="Secondary"/> are too dark/saturated.
    /// </summary>
    public static readonly MudColor RareBackground = "#a9c0cf";
}
