using QuestPDF.Helpers;

namespace OutSystems.QuestPdf.Internal;

internal static class PageSetup
{
    public static PageSize Resolve(string? name, bool landscape)
    {
        var size = (name?.Trim().ToUpperInvariant()) switch
        {
            "A3" => PageSizes.A3,
            "A5" => PageSizes.A5,
            "LETTER" => PageSizes.Letter,
            "LEGAL" => PageSizes.Legal,
            _ => PageSizes.A4,
        };

        return landscape ? size.Landscape() : size;
    }
}
