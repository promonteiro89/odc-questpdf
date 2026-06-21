using System;
using QuestPDF.Drawing;

namespace OutSystems.QuestPdf.Internal;

// Registers any .ttf/.otf embedded under Resources/Fonts (unregistered fonts fall
// back to the bundled Lato, silently, on a fontless container).
internal static class FontBootstrapper
{
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
