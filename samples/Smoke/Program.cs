using OutSystems.QuestPdf;
using OutSystems.QuestPdf.Models;

// Showcase of the builder API — every primitive, plus the navigation (TOC +
// sections), runtime font registration, and document metadata.
var g = new QuestPdfGenerator();

var doc = g.Create(new DocumentOptions { PageSize = "A4", MarginCm = 2m, FontFamily = "Lato", FontSize = 11m });
doc = g.SetMetadata(doc, "Feature Showcase", "ACME Corporation", "QuestPDF on ODC demo");
doc = g.SetWatermark(doc, "SAMPLE", new TextStyle());
doc = g.SetHeader(doc, "ACME CORPORATION", new TextStyle { Bold = true, FontSize = 10m, ColorHex = "#1f3b57", Alignment = "Right" });
doc = g.SetFooter(doc, "Confidential", new TextStyle { FontSize = 9m, ColorHex = "#888888", Alignment = "Left" }, showPageNumbers: true);

// Runtime font: register a brand font from bytes if one is available on this machine.
foreach (var p in new[] { "/System/Library/Fonts/Supplemental/Georgia.ttf", "/Library/Fonts/Arial.ttf" })
    if (File.Exists(p)) { doc = g.RegisterFont(doc, "Brand", File.ReadAllBytes(p)); break; }

doc = g.AddHeading(doc, "Feature Showcase", 1, new TextStyle { ColorHex = "#1f3b57", FontFamily = "Brand" });

// Clickable table of contents — entries jump to named sections, page numbers auto.
doc = g.AddHeading(doc, "Contents", 3, new TextStyle());
doc = g.AddTableOfContents(doc, new[]
{
    new TocEntry { Label = "Text, rich text & lists", SectionName = "sec-text" },
    new TocEntry { Label = "Layout & media", SectionName = "sec-layout" },
    new TocEntry { Label = "Tabular data", SectionName = "sec-table" },
}, new TextStyle { ColorHex = "#1a66cc" });

doc = g.AddPageBreak(doc);
doc = g.BeginSection(doc, "sec-text");
doc = g.AddHeading(doc, "Text, rich text & lists", 2, new TextStyle { ColorHex = "#1f3b57" });
doc = g.AddRichText(doc, new[]
{
    new TextSpan { Text = "Mixed " },
    new TextSpan { Text = "bold", Style = new TextStyle { Bold = true } },
    new TextSpan { Text = ", " },
    new TextSpan { Text = "red italic", Style = new TextStyle { Italic = true, ColorHex = "#cc2b2b" } },
    new TextSpan { Text = ", a " },
    new TextSpan { Text = "hyperlink", Style = new TextStyle { ColorHex = "#1a66cc", Underline = true }, Hyperlink = "https://www.questpdf.com" },
    new TextSpan { Text = ", and a " },
    new TextSpan { Text = "cross-reference", Style = new TextStyle { ColorHex = "#1a66cc" }, SectionLink = "sec-table" },
    new TextSpan { Text = "." },
}, "Justify");
doc = g.AddList(doc, new[] { "First point", "Second point", "Third point" }, ordered: false, new TextStyle());
doc = g.BeginBox(doc, new BoxStyle { BackgroundHex = "#eef4fb", BorderColorHex = "#1f3b57", BorderThickness = 1m, Padding = 10m });
doc = g.AddText(doc, "Callout: boxes apply background, border and padding around nested content.", new TextStyle());
doc = g.EndBox(doc);
doc = g.EndSection(doc);

doc = g.AddPageBreak(doc);
doc = g.BeginSection(doc, "sec-layout");
doc = g.AddHeading(doc, "Layout & media", 2, new TextStyle { ColorHex = "#1f3b57" });
doc = g.BeginRow(doc, 16m);
doc = g.BeginCell(doc, "Relative", 1m);
doc = g.AddText(doc, "Left column content flows here.", new TextStyle());
doc = g.EndCell(doc);
doc = g.BeginCell(doc, "Relative", 1m);
doc = g.AddSvg(doc, "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='60'><rect width='160' height='60' rx='6' fill='#1f3b57'/><circle cx='30' cy='30' r='18' fill='#f0a500'/><text x='58' y='37' fill='white' font-size='18' font-family='sans-serif'>Vector</text></svg>", new ImageOptions { Fit = "FitWidth" });
doc = g.EndCell(doc);
doc = g.EndRow(doc);
doc = g.EndSection(doc);

doc = g.AddPageBreak(doc);
doc = g.BeginSection(doc, "sec-table");
doc = g.AddHeading(doc, "Tabular data", 2, new TextStyle { ColorHex = "#1f3b57" });
doc = g.AddTable(doc, new TableSpec
{
    Columns = new[] { "Item", "Qty", "Total" },
    ColumnWidths = new[] { 3m, 1m, 1m },
    HeaderBackgroundHex = "#1f3b57",
    HeaderStyle = new TextStyle { ColorHex = "#ffffff", Bold = true },
    ShowBorders = true,
    Rows = new[]
    {
        new TableRow { Cells = new[] { "Widget", "2", "19.98" } },
        new TableRow
        {
            RichCells = new[]
            {
                new TableCell { Text = "Subtotal", ColumnSpan = 2, Alignment = "Right", Style = new TextStyle { Bold = true } },
                new TableCell { Text = "19.98", BackgroundHex = "#fff3a0", Style = new TextStyle { Bold = true } },
            },
        },
    },
});
doc = g.EndSection(doc);

var pdf = g.Render(doc);
var path = Path.Combine(AppContext.BaseDirectory, "out.pdf");
File.WriteAllBytes(path, pdf);
Console.WriteLine($"Wrote {pdf.Length:N0} bytes -> {path}");
