using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using OutSystems.QuestPdf.Internal;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OutSystems.QuestPdf.Rendering;

/// <summary>
/// Walks the document tree and renders it with QuestPDF. This is the only place
/// that touches the QuestPDF Fluent API.
/// </summary>
internal sealed class PdfComposer
{
    // Fonts register process-globally; track which names this process already
    // registered so repeated renders (warm Lambda) don't re-register.
    private static readonly HashSet<string> RegisteredFonts = new();
    private static readonly object FontGate = new();

    private readonly DocumentSpec _spec;

    public PdfComposer(DocumentSpec spec) => _spec = spec;

    public byte[] Render()
    {
        var open = _spec.OpenContainerType();
        if (open != null)
            throw new InvalidOperationException(
                $"The document has an unclosed {open} container. Add the matching End{open} action before rendering (every BeginRow/BeginColumn/BeginCell/BeginBox/BeginSection needs a matching End).");

        RegisterFonts();

        var o = _spec.Options;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSetup.Resolve(o.PageSize, o.Landscape));
                page.Margin((float)(o.MarginCm <= 0 ? 2 : o.MarginCm), Unit.Centimetre);
                page.DefaultTextStyle(t => t
                    .FontFamily(string.IsNullOrWhiteSpace(o.FontFamily) ? "Lato" : o.FontFamily!)
                    .FontSize(o.FontSize > 0 ? (float)o.FontSize : 11f));

                if (_spec.Watermark != null)
                    RenderWatermark(page.Background(), _spec.Watermark);

                if (_spec.Header != null)
                    RenderHeaderFooter(page.Header(), _spec.Header);

                RenderColumn(page.Content(), _spec.Root);

                if (_spec.Footer != null)
                    RenderHeaderFooter(page.Footer(), _spec.Footer);
            });
        });

        if (!string.IsNullOrWhiteSpace(_spec.Title) || !string.IsNullOrWhiteSpace(_spec.Author) || !string.IsNullOrWhiteSpace(_spec.Subject))
            document = document.WithMetadata(new DocumentMetadata
            {
                Title = _spec.Title ?? string.Empty,
                Author = _spec.Author ?? string.Empty,
                Subject = _spec.Subject ?? string.Empty,
            });

        try
        {
            return document.GeneratePdf();
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            throw new InvalidOperationException(
                "PDF rendering failed: " + ex.Message + " — check image/SVG bytes, hex colors and registered fonts.", ex);
        }
    }

    // ---- dispatch -----------------------------------------------------------

    private static readonly HttpClient FontHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

    private void RegisterFonts()
    {
        if (_spec.Fonts is not { Count: > 0 }) return;
        foreach (var f in _spec.Fonts)
        {
            if (string.IsNullOrWhiteSpace(f.Name)) continue;
            lock (FontGate)
            {
                if (RegisteredFonts.Contains(f.Name!)) continue; // already registered in this process

                var bytes = f.Data is { Length: > 0 } ? f.Data
                          : !string.IsNullOrWhiteSpace(f.Url) ? DownloadFont(f.Name!, f.Url!)
                          : null;
                if (bytes is not { Length: > 0 }) continue;

                using var ms = new MemoryStream(bytes);
                FontManager.RegisterFontWithCustomName(f.Name!, ms);
                RegisteredFonts.Add(f.Name!);
            }
        }
    }

    private static byte[] DownloadFont(string name, string url)
    {
        try
        {
            using var response = FontHttp.Send(new HttpRequestMessage(HttpMethod.Get, url));
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            using var stream = response.Content.ReadAsStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch font \"{name}\" from {url}: {ex.Message}", ex);
        }
    }

    private void RenderNode(IContainer c, DocNode n)
    {
        switch (n.Type)
        {
            case "Column":
            case "Cell": RenderColumn(c, n); break;
            case "Row": RenderRow(c, n); break;
            case "Box": RenderBox(c, n); break;
            case "Section":
                RenderColumn(string.IsNullOrWhiteSpace(n.Name) ? c : c.Section(n.Name!), n);
                break;
            case "Text": RenderText(c, n.Text, n.Style); break;
            case "RichText": RenderRichText(c, n); break;
            case "Heading": RenderHeading(c, n); break;
            case "List": RenderList(c, n); break;
            case "Toc": RenderToc(c, n); break;
            case "Image": RenderImage(c, n); break;
            case "Svg": RenderSvg(c, n); break;
            case "Table": if (n.Table != null) RenderTable(c, n.Table); break;
            case "Divider":
                c.LineHorizontal(n.Number > 0 ? (float)n.Number : 1f).LineColor(Col(n.ColorHex, Colors.Grey.Medium));
                break;
            case "Space": c.Height((float)Math.Max(0, n.Number)); break;
            case "PageBreak": c.PageBreak(); break;
        }
    }

    // ---- containers ---------------------------------------------------------

    private void RenderColumn(IContainer c, DocNode n)
    {
        c.Column(col =>
        {
            col.Spacing(n.Number > 0 ? (float)n.Number : 8f);
            foreach (var child in n.Children)
                RenderNode(col.Item(), child);
        });
    }

    private void RenderRow(IContainer c, DocNode n)
    {
        c.Row(row =>
        {
            row.Spacing(n.Number > 0 ? (float)n.Number : 8f);
            foreach (var child in n.Children)
            {
                if (child.Type == "Cell")
                {
                    var item = string.Equals(child.WidthType, "Constant", StringComparison.OrdinalIgnoreCase)
                        ? row.ConstantItem((float)(child.Width <= 0 ? 100 : child.Width))
                        : row.RelativeItem((float)(child.Width <= 0 ? 1 : child.Width));
                    RenderColumn(item, child);
                }
                else
                {
                    RenderNode(row.RelativeItem(), child);
                }
            }
        });
    }

    private void RenderBox(IContainer c, DocNode n)
    {
        var b = n.Box ?? new BoxStyleData();
        if (!string.IsNullOrWhiteSpace(b.BackgroundHex))
            c = c.Background(b.BackgroundHex!);
        if (b.BorderThickness > 0)
            c = c.Border((float)b.BorderThickness).BorderColor(Col(b.BorderColorHex, Colors.Grey.Medium));
        if (b.Padding > 0)
            c = c.Padding((float)b.Padding);
        RenderColumn(c, n);
    }

    // ---- leaves -------------------------------------------------------------

    private static void RenderText(IContainer c, string? text, StyleSpec? style)
    {
        c.Text(t =>
        {
            ApplyAlign(t, style?.Align);
            ApplySpanStyle(t.Span(text ?? string.Empty), style);
        });
    }

    private static void RenderRichText(IContainer c, DocNode n)
    {
        c.Text(t =>
        {
            ApplyAlign(t, n.Align);
            foreach (var sp in n.Spans ?? new List<SpanData>())
            {
                TextSpanDescriptor span;
                if (!string.IsNullOrWhiteSpace(sp.SectionLink))
                    span = t.SectionLink(sp.Text ?? string.Empty, sp.SectionLink!);
                else if (!string.IsNullOrWhiteSpace(sp.Hyperlink))
                    span = t.Hyperlink(sp.Text ?? string.Empty, sp.Hyperlink!);
                else
                    span = t.Span(sp.Text ?? string.Empty);
                ApplySpanStyle(span, sp.Style);
            }
        });
    }

    private static void RenderHeading(IContainer c, DocNode n)
    {
        var s = n.Style ?? new StyleSpec();
        var heading = new StyleSpec
        {
            FontFamily = s.FontFamily,
            FontSize = s.FontSize > 0 ? s.FontSize : n.Level switch { 1 => 20, 2 => 16, 3 => 13, _ => 16 },
            Bold = true,
            Italic = s.Italic,
            Underline = s.Underline,
            ColorHex = s.ColorHex,
            Align = s.Align,
            LineHeight = s.LineHeight,
        };
        RenderText(c, n.Text, heading);
    }

    private static void RenderList(IContainer c, DocNode n)
    {
        var items = n.Items ?? new List<string>();
        c.Column(col =>
        {
            col.Spacing(4);
            for (var i = 0; i < items.Count; i++)
            {
                var marker = n.Ordered ? $"{i + 1}." : "•";
                var text = items[i];
                col.Item().Row(r =>
                {
                    r.Spacing(6);
                    r.ConstantItem(22).Text(t => ApplySpanStyle(t.Span(marker), n.Style));
                    r.RelativeItem().Text(t => ApplySpanStyle(t.Span(text ?? string.Empty), n.Style));
                });
            }
        });
    }

    private static void RenderToc(IContainer c, DocNode n)
    {
        var entries = n.Toc ?? new List<TocEntryData>();
        c.Column(col =>
        {
            col.Spacing(5);
            foreach (var e in entries)
            {
                var name = e.SectionName ?? string.Empty;
                col.Item().Row(row =>
                {
                    if (e.Indent > 0)
                        row.ConstantItem(e.Indent * 16);
                    row.RelativeItem().Text(t => ApplySpanStyle(t.SectionLink(e.Label ?? string.Empty, name), n.Style));
                    row.ConstantItem(42).AlignRight().Text(t => ApplySpanStyle(t.BeginPageNumberOfSection(name), n.Style));
                });
            }
        });
    }

    private static void RenderImage(IContainer c, DocNode n)
    {
        if (n.Image is not { Length: > 0 }) return;
        var box = AlignAndConstrain(c, n);
        var image = box.Image(n.Image);
        switch (n.Fit)
        {
            case "FitArea": image.FitArea(); break;
            case "Original": break;
            default: image.FitWidth(); break;
        }
    }

    private static void RenderSvg(IContainer c, DocNode n)
    {
        if (string.IsNullOrWhiteSpace(n.Svg)) return;
        var box = AlignAndConstrain(c, n);
        var svg = box.Svg(n.Svg!);
        switch (n.Fit)
        {
            case "FitArea": svg.FitArea(); break;
            case "Original": break;
            default: svg.FitWidth(); break;
        }
    }

    private static IContainer AlignAndConstrain(IContainer c, DocNode n)
    {
        var box = n.Align switch
        {
            "Center" => c.AlignCenter(),
            "Right" => c.AlignRight(),
            _ => c,
        };
        if (n.MaxWidth > 0)
            box = box.MaxWidth((float)n.MaxWidth);
        return box;
    }

    private static void RenderTable(IContainer c, TableSpecData t)
    {
        var columns = t.Columns ?? new();
        var rows = t.Rows ?? new();

        var colCount = columns.Count;
        foreach (var row in rows)
        {
            var width = row.RichCells is { Count: > 0 }
                ? row.RichCells.Sum(rc => Math.Max(1, rc.ColumnSpan))
                : (row.Cells?.Count ?? 0);
            colCount = Math.Max(colCount, width);
        }
        if (colCount == 0) return;

        c.Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                for (var i = 0; i < colCount; i++)
                {
                    var w = (t.ColumnWidths != null && i < t.ColumnWidths.Count && t.ColumnWidths[i] > 0)
                        ? (float)t.ColumnWidths[i]
                        : 1f;
                    def.RelativeColumn(w);
                }
            });

            if (columns.Count > 0)
            {
                var headerStyle = t.HeaderStyle ?? new StyleSpec { Bold = true };
                table.Header(header =>
                {
                    for (var i = 0; i < colCount; i++)
                    {
                        var text = i < columns.Count ? columns[i] : string.Empty;
                        header.Cell().Element(x => HeaderCellStyle(x, t))
                            .Text(tt => ApplySpanStyle(tt.Span(text ?? string.Empty), headerStyle));
                    }
                });
            }

            foreach (var row in rows)
            {
                if (row.RichCells is { Count: > 0 })
                    RenderRichRow(table, row.RichCells, t, colCount);
                else
                    RenderPlainRow(table, row.Cells ?? new(), t, colCount);
            }
        });
    }

    private static void RenderPlainRow(TableDescriptor table, List<string> cells, TableSpecData t, int colCount)
    {
        for (var i = 0; i < colCount; i++)
        {
            var value = i < cells.Count ? cells[i] : string.Empty;
            table.Cell().Element(x => BodyCellStyle(x, t, null))
                .Text(tt => ApplySpanStyle(tt.Span(value ?? string.Empty), t.BodyStyle));
        }
    }

    private static void RenderRichRow(TableDescriptor table, List<TableCellData> cells, TableSpecData t, int colCount)
    {
        var consumed = 0;
        foreach (var cell in cells)
        {
            var span = Math.Max(1, cell.ColumnSpan);
            var c = table.Cell();
            if (span > 1) c = c.ColumnSpan((uint)span);
            c.Element(x => BodyCellStyle(x, t, cell.BackgroundHex))
                .Text(tt =>
                {
                    ApplyAlign(tt, cell.Align);
                    ApplySpanStyle(tt.Span(cell.Text ?? string.Empty), cell.Style ?? t.BodyStyle);
                });
            consumed += span;
        }
        for (; consumed < colCount; consumed++)
            table.Cell().Element(x => BodyCellStyle(x, t, null)).Text(string.Empty);
    }

    private void RenderHeaderFooter(IContainer c, HeaderFooterSpec hf)
    {
        c.Column(col =>
        {
            col.Spacing(2);
            if (!string.IsNullOrWhiteSpace(hf.Text))
                RenderText(col.Item(), hf.Text, hf.Style);

            if (hf.ShowPageNumbers)
                col.Item().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium));
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
        });
    }

    private static void RenderWatermark(IContainer c, WatermarkSpec w)
    {
        var s = w.Style ?? new StyleSpec();
        var style = new StyleSpec
        {
            FontFamily = s.FontFamily,
            FontSize = s.FontSize > 0 ? s.FontSize : 90,
            Bold = true,
            ColorHex = string.IsNullOrWhiteSpace(s.ColorHex) ? "#D8DEE6" : s.ColorHex,
        };
        c.AlignCenter().AlignMiddle().Rotate(-45).Text(t => ApplySpanStyle(t.Span(w.Text ?? string.Empty), style));
    }

    // ---- styling helpers ----------------------------------------------------

    private static void ApplyAlign(TextDescriptor t, string? align)
    {
        switch ((align ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "center": t.AlignCenter(); break;
            case "right": t.AlignRight(); break;
            case "justify": t.Justify(); break;
            default: t.AlignLeft(); break;
        }
    }

    private static void ApplySpanStyle(TextSpanDescriptor span, StyleSpec? s)
    {
        if (s == null) return;
        if (!string.IsNullOrWhiteSpace(s.FontFamily)) span.FontFamily(s.FontFamily!);
        if (s.FontSize > 0) span.FontSize((float)s.FontSize);
        if (s.Bold) span.Bold();
        if (s.Italic) span.Italic();
        if (s.Underline) span.Underline();
        if (s.Strike) span.Strikethrough();
        if (s.Super) span.Superscript();
        if (s.Sub) span.Subscript();
        var fc = Validate.HexColor(s.ColorHex, "TextStyle.ColorHex");
        if (fc != null) span.FontColor(fc);
        var bg = Validate.HexColor(s.BgHex, "TextStyle.BackgroundColorHex");
        if (bg != null) span.BackgroundColor(bg);
        if (s.LineHeight > 0) span.LineHeight((float)s.LineHeight);
    }

    private static IContainer HeaderCellStyle(IContainer c, TableSpecData t)
    {
        c = c.Background(Col(t.HeaderBackgroundHex, Colors.Grey.Lighten2));
        c = t.ShowBorders
            ? c.Border(0.5f).BorderColor(Colors.Grey.Medium)
            : c.BorderBottom(1).BorderColor(Colors.Grey.Medium);
        return c.PaddingVertical(5).PaddingHorizontal(6);
    }

    private static IContainer BodyCellStyle(IContainer c, TableSpecData t, string? backgroundHex)
    {
        if (!string.IsNullOrWhiteSpace(backgroundHex))
            c = c.Background(backgroundHex!);
        c = t.ShowBorders
            ? c.Border(0.5f).BorderColor(Colors.Grey.Lighten1)
            : c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
        return c.PaddingVertical(4).PaddingHorizontal(6);
    }

    private static Color Col(string? hex, Color fallback)
    {
        var v = Validate.HexColor(hex, "color");
        return v == null ? fallback : v;
    }
}
