using System.Globalization;

namespace Jordnaer.Extensions;

public static class ColorExtensions
{
    /// <summary>
    /// Determines if a hex color is perceived as light based on brightness.
    /// Supports both #RGB and #RRGGBB formats.
    /// </summary>
    /// <param name="hexColor">The hex color string (e.g., "#FFFFFF" or "#FFF").</param>
    /// <returns>True if the color is perceived as light, false otherwise.</returns>
    public static bool IsLightColor(string hexColor)
    {
        if (!TryParseHexColor(hexColor, out var r, out var g, out var b))
        {
            return false;
        }

        // Using perceived brightness formula (ITU-R BT.601)
        var brightness = (r * 299 + g * 587 + b * 114) / 1000;
        return brightness > 128;
    }

    /// <summary>
    /// Gets the appropriate contrasting text color for a given background color.
    /// </summary>
    /// <param name="hexBackgroundColor">The hex background color string.</param>
    /// <returns>A CSS color string suitable for text on the given background.</returns>
    public static string GetContrastingTextColor(string hexBackgroundColor)
    {
        return IsLightColor(hexBackgroundColor)
            ? "rgba(0, 0, 0, 0.7)"
            : "rgba(255, 255, 255, 0.9)";
    }

    private static bool TryParseHexColor(string hexColor, out int r, out int g, out int b)
    {
        r = g = b = 0;

        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith('#'))
        {
            return false;
        }

        if (hexColor.Length == 7)
        {
            // #RRGGBB format
            return int.TryParse(hexColor.AsSpan(1, 2), NumberStyles.HexNumber, null, out r) &&
                   int.TryParse(hexColor.AsSpan(3, 2), NumberStyles.HexNumber, null, out g) &&
                   int.TryParse(hexColor.AsSpan(5, 2), NumberStyles.HexNumber, null, out b);
        }

        if (hexColor.Length == 4)
        {
            // #RGB format - expand each digit (e.g., #F0A becomes #FF00AA)
            if (!int.TryParse(hexColor.AsSpan(1, 1), NumberStyles.HexNumber, null, out r) ||
                !int.TryParse(hexColor.AsSpan(2, 1), NumberStyles.HexNumber, null, out g) ||
                !int.TryParse(hexColor.AsSpan(3, 1), NumberStyles.HexNumber, null, out b))
            {
                return false;
            }

            // Expand single hex digit to double (0xF -> 0xFF, 0xA -> 0xAA)
            r *= 17;
            g *= 17;
            b *= 17;
            return true;
        }

        return false;
    }
}
