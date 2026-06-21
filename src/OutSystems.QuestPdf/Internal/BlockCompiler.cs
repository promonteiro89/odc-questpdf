using System;
using System.Collections.Generic;
using System.Linq;
using OutSystems.QuestPdf.Models;

namespace OutSystems.QuestPdf.Internal;

// Builds the render tree from the Block list; Begin*/End* open and close
// containers. Errors are tagged with the block index.
internal static class BlockCompiler
{
    public static DocumentSpec Compile(DocumentOptions options, IEnumerable<Block>? content)
    {
        var spec = new DocumentSpec
        {
            Options = OptionsSpec.From(options),
            Title = StyleSpec.Blank(options.Title),
            Author = StyleSpec.Blank(options.Author),
            Subject = StyleSpec.Blank(options.Subject),
            Header = HeaderFooterSpec.From(options.Header, isFooter: false),
            Footer = HeaderFooterSpec.From(options.Footer, isFooter: true),
            Watermark = WatermarkSpec.From(options.Watermark),
            Fonts = CompileFonts(options.Fonts),
        };

        var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var block in content ?? Enumerable.Empty<Block>())
        {
            var type = (block.Type ?? string.Empty).Trim();
            try
            {
                CompileBlock(spec, block, type, sections);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Block #{index} ('{(type.Length == 0 ? "?" : type)}'): {ex.Message}", ex);
            }
            index++;
        }

        var open = spec.OpenContainerType();
        if (open != null)
            throw new InvalidOperationException(
                $"Unclosed '{open}' block — add the matching 'End{open}' block before the end of the content.");

        return spec;
    }

    private static void CompileBlock(DocumentSpec spec, Block b, string type, HashSet<string> sections)
    {
        switch (type)
        {
            case "Heading":
                spec.Append(new DocNode { Type = "Heading", Text = b.Text, Level = b.Level <= 0 ? 1 : b.Level, Style = StyleSpec.From(b.Style) });
                break;
            case "Text":
                spec.Append(new DocNode { Type = "Text", Text = b.Text, Style = StyleSpec.From(b.Style) });
                break;
            case "RichText":
                spec.Append(new DocNode { Type = "RichText", Align = StyleSpec.Blank(b.Alignment), Spans = (b.Spans ?? Enumerable.Empty<TextSpan>()).Select(SpanData.From).ToList() });
                break;
            case "List":
                spec.Append(new DocNode { Type = "List", Ordered = b.Ordered, Style = StyleSpec.From(b.Style), Items = (b.Items ?? Enumerable.Empty<string>()).ToList() });
                break;
            case "Image":
                Validate.ImageBytes(b.Image, "Image block 'Image'");
                spec.Append(new DocNode { Type = "Image", Image = b.Image, Fit = StyleSpec.Blank(b.Fit), MaxWidth = (double)b.MaxWidthPoints, Align = StyleSpec.Blank(b.Alignment) });
                break;
            case "Svg":
                if (string.IsNullOrWhiteSpace(b.Svg)) throw new ArgumentException("Svg block requires 'Svg' markup.");
                spec.Append(new DocNode { Type = "Svg", Svg = b.Svg, Fit = StyleSpec.Blank(b.Fit), MaxWidth = (double)b.MaxWidthPoints, Align = StyleSpec.Blank(b.Alignment) });
                break;
            case "Table":
                spec.Append(new DocNode { Type = "Table", Table = TableSpecData.From(b.Table) });
                break;
            case "Toc":
                spec.Append(new DocNode { Type = "Toc", Style = StyleSpec.From(b.Style), Toc = (b.Toc ?? Enumerable.Empty<TocEntry>()).Select(TocEntryData.From).ToList() });
                break;
            case "Divider":
                spec.Append(new DocNode { Type = "Divider", ColorHex = Validate.HexColor(b.ColorHex, "Divider 'ColorHex'"), Number = (double)b.Thickness });
                break;
            case "Space":
                spec.Append(new DocNode { Type = "Space", Number = (double)b.Height });
                break;
            case "PageBreak":
                spec.Append(new DocNode { Type = "PageBreak" });
                break;

            case "BeginRow":
                spec.Open(new DocNode { Type = "Row", Number = (double)b.Spacing });
                break;
            case "EndRow":
                spec.Close("Row");
                break;
            case "BeginColumn":
                spec.Open(new DocNode { Type = "Column", Number = (double)b.Spacing });
                break;
            case "EndColumn":
                spec.Close("Column");
                break;
            case "BeginCell":
                spec.Open(new DocNode
                {
                    Type = "Cell",
                    WidthType = string.Equals(b.WidthType?.Trim(), "Constant", StringComparison.OrdinalIgnoreCase) ? "Constant" : "Relative",
                    Width = (double)b.Width,
                });
                break;
            case "EndCell":
                spec.Close("Cell");
                break;
            case "BeginBox":
                spec.Open(new DocNode { Type = "Box", Box = BoxStyleData.From(b.Box) });
                break;
            case "EndBox":
                spec.Close("Box");
                break;
            case "BeginSection":
                {
                    var name = (b.SectionName ?? string.Empty).Trim();
                    if (name.Length == 0) throw new ArgumentException("BeginSection requires a 'SectionName'.");
                    if (!sections.Add(name)) throw new ArgumentException($"Section name \"{name}\" is used more than once; names must be unique.");
                    spec.Open(new DocNode { Type = "Section", Name = name });
                }
                break;
            case "EndSection":
                spec.Close("Section");
                break;

            case "":
                throw new ArgumentException("missing 'Type'.");
            default:
                throw new ArgumentException($"unknown block type \"{type}\".");
        }
    }

    private static List<FontData>? CompileFonts(IEnumerable<FontRef>? fonts)
    {
        if (fonts == null) return null;
        var list = new List<FontData>();
        foreach (var f in fonts)
        {
            if (string.IsNullOrWhiteSpace(f.Name))
                throw new ArgumentException("A FontRef requires a 'Name'.");
            var hasBytes = f.Bytes is { Length: > 0 };
            var hasUrl = !string.IsNullOrWhiteSpace(f.Url);
            if (!hasBytes && !hasUrl)
                throw new ArgumentException($"FontRef \"{f.Name}\" needs a 'Url' or 'Bytes'.");
            if (hasUrl && (!Uri.TryCreate(f.Url!.Trim(), UriKind.Absolute, out var uri)
                           || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
                throw new ArgumentException($"FontRef \"{f.Name}\" has an invalid URL \"{f.Url}\" (must be absolute http/https).");
            list.Add(new FontData { Name = f.Name.Trim(), Data = hasBytes ? f.Bytes : null, Url = hasUrl ? f.Url!.Trim() : null });
        }
        return list;
    }
}
