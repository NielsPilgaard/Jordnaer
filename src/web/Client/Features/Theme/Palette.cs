using MudBlazor.Utilities;

namespace Jordnaer.Client.Features.Theme;

public static class Palette
{
    /// <summary>
    /// Used as background for texts and headers
    /// </summary>
    public static readonly MudColor Primary = "#dbab45";

    /// <summary>
    /// Used as background for texts and headers
    /// </summary>
    public static readonly MudColor Secondary = "#878e64";

    /// <summary>
    /// Used for body text
    /// </summary>
    public static readonly MudColor Body = "#41556b";

    /// <summary>
    /// Used for small texts, payoffs, quotes
    /// </summary>
    public static readonly MudColor Header = "#673417";

    /// <summary>
    /// Used as background for text where <see cref="Primary"/> and <see cref="Secondary"/> are too dark/saturated.
    /// </summary>
    public static readonly MudColor DimBackground = "#cfc1a6";

    /// <summary>
    /// Rarely used as background for text where <see cref="Primary"/> and <see cref="Secondary"/> are too dark/saturated.
    /// </summary>
    public static readonly MudColor RareBackground = "#a9c0cf";
}
