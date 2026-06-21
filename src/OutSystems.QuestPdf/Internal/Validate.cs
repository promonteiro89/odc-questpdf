using System;
using System.Text.RegularExpressions;

namespace OutSystems.QuestPdf.Internal;

// Input validation with clear messages (surfaced at render, tagged with the block index).
internal static class Validate
{
    private static readonly Regex HexPattern =
        new("^#?([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

    // Returns a normalized "#RRGGBB"/"#AARRGGBB", or null for blank; throws on invalid.
    public static string? HexColor(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        if (!HexPattern.IsMatch(v))
            throw new ArgumentException(
                $"Invalid hex color \"{value}\" for {field}. Expected \"#RRGGBB\" or \"#AARRGGBB\" (for example \"#1f3b57\").");
        return v[0] == '#' ? v : "#" + v;
    }

    public static void ImageBytes(byte[]? data, string field)
    {
        if (data is not { Length: > 3 })
            throw new ArgumentException($"{field} requires non-empty image bytes.");

        bool ok =
            (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) ||                 // PNG
            (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) ||                                    // JPEG
            (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46) ||                                    // GIF
            (data[0] == 0x42 && data[1] == 0x4D) ||                                                       // BMP
            (data.Length > 11 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 // WEBP (RIFF....WEBP)
                              && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) ||
            (data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00) ||                 // TIFF (LE)
            (data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A);                   // TIFF (BE)

        if (!ok)
            throw new ArgumentException($"{field} doesn't look like a supported image (PNG, JPEG, GIF, BMP, WEBP or TIFF).");
    }
}
