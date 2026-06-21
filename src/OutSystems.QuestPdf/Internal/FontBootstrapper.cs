using System;
using QuestPDF.Drawing;

namespace OutSystems.QuestPdf.Internal;

/// <summary>
/// Registers every .ttf/.otf embedded under Resources/Fonts so the library is
/// independent of host (container) fonts. On a fontless ODC container, an
/// unregistered font silently falls back to QuestPDF's bundled Lato — it does
/// NOT throw — so always verify the actual glyphs you need.
/// </summary>
internal static class FontBootstrapper
{
    /// <summary>Returns the number of fonts registered.</summary>
    public static int RegisterEmbeddedFonts()
    {
        var count = 0;
        var asm = typeof(FontBootstrapper).Assembly;

        foreach (var resource in asm.GetManifestResourceNames())
        {
            if (!resource.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) &&
                !resource.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = asm.GetManifestResourceStream(resource);
            if (stream is null) continue;

            FontManager.RegisterFont(stream);
            count++;
        }

        return count;
    }
}
