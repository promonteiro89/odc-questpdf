using System;
using System.Collections.Generic;
using System.Linq;
using OutSystems.QuestPdf.Models;

namespace OutSystems.QuestPdf.Internal;

// Internal render model: built by BlockCompiler, consumed by PdfComposer.

internal sealed class StyleSpec
{
    public string? FontFamily { get; set; }
    public double FontSize { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strike { get; set; }
    public bool Super { get; set; }
    public bool Sub { get; set; }
    public string? ColorHex { get; set; }
    public string? BgHex { get; set; }
    public string? Align { get; set; }
    public double LineHeight { get; set; }

    public static StyleSpec From(TextStyle s) => new()
    {
        FontFamily = Blank(s.FontFamily),
        FontSize = (double)s.FontSize,
        Bold = s.Bold,
        Italic = s.Italic,
        Underline = s.Underline,
        Strike = s.Strikethrough,
        Super = s.Superscript,
        Sub = s.Subscript,
        ColorHex = Validate.HexColor(s.ColorHex, "TextStyle.ColorHex"),
        BgHex = Validate.HexColor(s.BackgroundColorHex, "TextStyle.BackgroundColorHex"),
        Align = Blank(s.Alignment),
        LineHeight = (double)s.LineHeight,
    };

    internal static string? Blank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}

internal sealed class OptionsSpec
{
    public string? PageSize { get; set; }
    public bool Landscape { get; set; }
    public double MarginCm { get; set; }
    public string? FontFamily { get; set; }
    public double FontSize { get; set; }

    public static OptionsSpec From(DocumentOptions o) => new()
    {
        PageSize = o.PageSize,
        Landscape = o.Landscape,
        MarginCm = (double)o.MarginCm,
        FontFamily = o.FontFamily,
        FontSize = (double)o.FontSize,
    };
}

internal sealed class SpanData
{
    public string? Text { get; set; }
    public StyleSpec? Style { get; set; }
    public string? Hyperlink { get; set; }
    public string? SectionLink { get; set; }

    public static SpanData From(TextSpan s) => new()
    {
        Text = s.Text,
        Style = StyleSpec.From(s.Style),
        Hyperlink = StyleSpec.Blank(s.Hyperlink),
        SectionLink = StyleSpec.Blank(s.SectionLink),
    };
}

internal sealed class TocEntryData
{
    public string? Label { get; set; }
    public string? SectionName { get; set; }
    public int Indent { get; set; }

    public static TocEntryData From(TocEntry e) => new()
    {
        Label = e.Label,
        SectionName = StyleSpec.Blank(e.SectionName),
        Indent = e.Indent < 0 ? 0 : e.Indent,
    };
}

internal sealed class FontData
{
    public string? Name { get; set; }
    public byte[]? Data { get; set; }
    public string? Url { get; set; }
}

internal sealed class TableCellData
{
    public string? Text { get; set; }
    public StyleSpec? Style { get; set; }
    public string? BackgroundHex { get; set; }
    public string? Align { get; set; }
    public int ColumnSpan { get; set; }

    public static TableCellData From(TableCell c) => new()
    {
        Text = c.Text,
        Style = StyleSpec.From(c.Style),
        BackgroundHex = Validate.HexColor(c.BackgroundHex, "TableCell.BackgroundHex"),
        Align = StyleSpec.Blank(c.Alignment),
        ColumnSpan = c.ColumnSpan,
    };
}

internal sealed class TableRowData
{
    public List<string> Cells { get; set; } = new();
    public List<TableCellData> RichCells { get; set; } = new();
}

internal sealed class TableSpecData
{
    public List<string> Columns { get; set; } = new();
    public List<double> ColumnWidths { get; set; } = new();
    public List<TableRowData> Rows { get; set; } = new();
    public StyleSpec? HeaderStyle { get; set; }
    public StyleSpec? BodyStyle { get; set; }
    public string? HeaderBackgroundHex { get; set; }
    public bool ShowBorders { get; set; }

    public static TableSpecData From(TableSpec t) => new()
    {
        Columns = (t.Columns ?? Enumerable.Empty<string>()).ToList(),
        ColumnWidths = (t.ColumnWidths ?? Enumerable.Empty<decimal>()).Select(d => (double)d).ToList(),
        Rows = (t.Rows ?? Enumerable.Empty<TableRow>()).Select(r => new TableRowData
        {
            Cells = (r.Cells ?? Enumerable.Empty<string>()).ToList(),
            RichCells = (r.RichCells ?? Enumerable.Empty<TableCell>()).Select(TableCellData.From).ToList(),
        }).ToList(),
        HeaderStyle = StyleSpec.From(t.HeaderStyle),
        BodyStyle = StyleSpec.From(t.BodyStyle),
        HeaderBackgroundHex = Validate.HexColor(t.HeaderBackgroundHex, "TableSpec.HeaderBackgroundHex"),
        ShowBorders = t.ShowBorders,
    };
}

internal sealed class BoxStyleData
{
    public string? BackgroundHex { get; set; }
    public string? BorderColorHex { get; set; }
    public double BorderThickness { get; set; }
    public double Padding { get; set; }

    public static BoxStyleData From(BoxStyle b) => new()
    {
        BackgroundHex = Validate.HexColor(b.BackgroundHex, "BoxStyle.BackgroundHex"),
        BorderColorHex = Validate.HexColor(b.BorderColorHex, "BoxStyle.BorderColorHex"),
        BorderThickness = (double)b.BorderThickness,
        Padding = (double)b.Padding,
    };
}

internal sealed class HeaderFooterSpec
{
    public string? Text { get; set; }
    public StyleSpec? Style { get; set; }
    public bool ShowPageNumbers { get; set; }

    public static HeaderFooterSpec? From(HeaderFooter hf, bool isFooter)
    {
        var hasText = !string.IsNullOrWhiteSpace(hf.Text);
        var pageNumbers = isFooter && hf.ShowPageNumbers;
        if (!hasText && !pageNumbers) return null;
        return new HeaderFooterSpec
        {
            Text = StyleSpec.Blank(hf.Text),
            Style = StyleSpec.From(hf.Style),
            ShowPageNumbers = pageNumbers,
        };
    }
}

internal sealed class WatermarkSpec
{
    public string? Text { get; set; }
    public StyleSpec? Style { get; set; }

    public static WatermarkSpec? From(Watermark w)
    {
        if (string.IsNullOrWhiteSpace(w.Text)) return null;
        return new WatermarkSpec { Text = w.Text, Style = StyleSpec.From(w.Style) };
    }
}

internal sealed class DocNode
{
    // "Column" | "Row" | "Cell" | "Box" | "Section" | "Text" | "RichText" | "Heading"
    // | "List" | "Image" | "Svg" | "Table" | "Divider" | "Space" | "PageBreak" | "Toc"
    public string Type { get; set; } = "Column";

    public string? Text { get; set; }
    public int Level { get; set; }
    public StyleSpec? Style { get; set; }

    public List<SpanData>? Spans { get; set; }
    public List<string>? Items { get; set; }
    public bool Ordered { get; set; }
    public string? Name { get; set; }
    public List<TocEntryData>? Toc { get; set; }

    public byte[]? Image { get; set; }
    public string? Svg { get; set; }
    public string? Fit { get; set; }
    public double MaxWidth { get; set; }
    public string? Align { get; set; }

    public TableSpecData? Table { get; set; }
    public BoxStyleData? Box { get; set; }

    public double Number { get; set; } // spacing / space height / divider thickness
    public string? ColorHex { get; set; }
    public string? WidthType { get; set; }
    public double Width { get; set; }

    public List<DocNode> Children { get; set; } = new();
}

internal sealed class DocumentSpec
{
    public OptionsSpec Options { get; set; } = new();
    public DocNode Root { get; set; } = new() { Type = "Column" };
    public List<int> Cursor { get; set; } = new();
    public HeaderFooterSpec? Header { get; set; }
    public HeaderFooterSpec? Footer { get; set; }
    public WatermarkSpec? Watermark { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public List<FontData>? Fonts { get; set; }

    private DocNode CurrentContainer()
    {
        var node = Root;
        foreach (var index in Cursor)
            node = node.Children[index];
        return node;
    }

    public void Append(DocNode node) => CurrentContainer().Children.Add(node);

    public void Open(DocNode container)
    {
        var parent = CurrentContainer();
        if (container.Type == "Cell" && parent.Type != "Row")
            throw new InvalidOperationException("A 'BeginCell' block must be inside a 'BeginRow'.");
        parent.Children.Add(container);
        Cursor.Add(parent.Children.Count - 1);
    }

    public void Close(string expectedType)
    {
        if (Cursor.Count == 0)
            throw new InvalidOperationException($"'End{expectedType}' block has no matching 'Begin{expectedType}'.");
        var open = CurrentContainer();
        if (!string.Equals(open.Type, expectedType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Mismatched blocks: 'End{expectedType}' closes a {open.Type}. Check your Begin/End ordering.");
        Cursor.RemoveAt(Cursor.Count - 1);
    }

    public string? OpenContainerType()
    {
        if (Cursor.Count == 0) return null;
        var node = Root;
        foreach (var index in Cursor)
        {
            if (index < 0 || index >= node.Children.Count) return null;
            node = node.Children[index];
        }
        return node.Type;
    }
}
