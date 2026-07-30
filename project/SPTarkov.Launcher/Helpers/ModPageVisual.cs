using System.Globalization;

namespace SPTarkov.Launcher.Helpers;

/// <summary>
/// Build visual identities for nav-rail tiles. Both the badge colour and its initials are derived purely from the display name, so a given
/// mod always renders the same badge across sessions/machines.
/// </summary>
public static class ModPageVisual
{
    /// <summary>Returns a CSS hex colour derived from the name. Hue varies with the name; saturation and lightness are fixed.</summary>
    public static string GetColor(string name)
    {
        var hue = Hash(name) % 360u;
        return HslToHex(hue, 0.55, 0.45);
    }

    /// <summary>
    /// Returns 1-2 uppercase initials. The first letters of the first two words, or the first two characters of a single word.
    /// </summary>
    public static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "??";
        }

        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2)
        {
            return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}";
        }

        var word = words[0];
        return (word.Length >= 2 ? word[..2] : word).ToUpperInvariant();
    }

    // Deterministic FNV-1a 32-bit hash.
    private static uint Hash(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }

    // Converts an HSL colour to a hex string.
    private static string HslToHex(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = l - c / 2;

        var (r, g, b) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        var red = (int)Math.Round((r + m) * 255);
        var green = (int)Math.Round((g + m) * 255);
        var blue = (int)Math.Round((b + m) * 255);
        return string.Create(CultureInfo.InvariantCulture, $"#{red:x2}{green:x2}{blue:x2}");
    }
}
