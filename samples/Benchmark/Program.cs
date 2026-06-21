using System.Diagnostics;
using System.Globalization;
using System.Text;
using OutSystems.QuestPdf;
using OutSystems.QuestPdf.Models;

// Benchmark: build ONE complex report from identical data, two ways:
//   1) QuestPDF, via this ODC component (code-first, native Skia)
//   2) the same content as HTML, for Chromium HTML->PDF (UltimatePDF's engine)
// Usage: dotnet run --project samples/Benchmark -- <outDir> [rowCount] [warmIterations]

var outDir = args.Length > 0 ? args[0] : ".";
var rowCount = args.Length > 1 ? int.Parse(args[1]) : 600;
var warm = args.Length > 2 ? int.Parse(args[2]) : 10;
Directory.CreateDirectory(outDir);
var ci = CultureInfo.InvariantCulture;

// ---- deterministic dataset (identical for both renderers) -------------------
string[] categories = { "Retail", "Wholesale", "Online", "Services", "Logistics" };
string[] statuses = { "Paid", "Pending", "Overdue" };
var rows = new List<string[]>(rowCount);
decimal total = 0; int overdue = 0;
var monthly = new double[12];
for (var i = 0; i < rowCount; i++)
{
    var day = new DateTime(2026, 1, 1).AddDays(i % 180);
    var amount = ((i * 37) % 900) + 50 + 0.99m;
    var status = statuses[i % 3];
    total += amount;
    if (status == "Overdue") overdue++;
    monthly[day.Month - 1] += (double)amount;
    rows.Add(new[]
    {
        (i + 1).ToString(ci),
        day.ToString("yyyy-MM-dd", ci),
        $"Invoice #{1000 + i} — {categories[i % 5]} order",
        categories[i % 5],
        $"ACC-{2000 + (i * 7) % 500}",
        amount.ToString("N2", ci),
        status,
    });
}
var kpiCards = new (string Value, string Label)[]
{
    (total.ToString("N0", ci) + " EUR", "Total billed"),
    (rowCount.ToString("N0", ci), "Transactions"),
    ((total / rowCount).ToString("N2", ci) + " EUR", "Average invoice"),
    (overdue.ToString("N0", ci), "Overdue"),
};

string[] cols = { "#", "Date", "Description", "Category", "Account", "Amount (EUR)", "Status" };
decimal[] widths = { 0.6m, 1.3m, 3.0m, 1.3m, 1.3m, 1.3m, 1.1m };
var chartSvg = BuildChartSvg(monthly);
var logoSvg = "<svg xmlns='http://www.w3.org/2000/svg' width='150' height='50'>" +
              "<rect width='150' height='50' rx='6' fill='#1f3b57'/>" +
              "<circle cx='28' cy='25' r='14' fill='#f0a500'/>" +
              "<text x='52' y='32' fill='white' font-size='18' font-family='sans-serif'>ACME</text></svg>";

// ============================ QuestPDF =======================================
var g = new QuestPdfGenerator();

byte[] BuildHandle()
{
    var doc = g.Create(new DocumentOptions { PageSize = "A4", MarginCm = 1.6m, FontFamily = "Lato", FontSize = 10m });
    doc = g.SetWatermark(doc, "CONFIDENTIAL", new TextStyle());
    doc = g.SetHeader(doc, "ACME CORPORATION — Q2 2026 Report", new TextStyle { FontSize = 9m, ColorHex = "#1f3b57", Alignment = "Right", Bold = true });
    doc = g.SetFooter(doc, "Confidential", new TextStyle { FontSize = 8m, ColorHex = "#888888", Alignment = "Left" }, showPageNumbers: true);

    // Cover
    doc = g.AddSvg(doc, logoSvg, new ImageOptions { Fit = "Original" });
    doc = g.AddSpace(doc, 10m);
    doc = g.AddHeading(doc, "Q2 2026 Financial & Operations Report", 1, new TextStyle { ColorHex = "#1f3b57" });
    doc = g.AddText(doc, "Prepared 21 June 2026 · Finance & Operations", new TextStyle { ColorHex = "#666666" });
    doc = g.AddSpace(doc, 12m);
    doc = g.AddRichText(doc, new[]
    {
        new TextSpan { Text = "This report summarises " },
        new TextSpan { Text = $"{rowCount:N0} transactions", Style = new TextStyle { Bold = true } },
        new TextSpan { Text = " across all channels. Totals are " },
        new TextSpan { Text = "preliminary", Style = new TextStyle { Italic = true, ColorHex = "#cc2b2b" } },
        new TextSpan { Text = " and subject to audit. See " },
        new TextSpan { Text = "questpdf.com", Style = new TextStyle { ColorHex = "#1a66cc", Underline = true }, Hyperlink = "https://www.questpdf.com" },
        new TextSpan { Text = "." },
    }, "Justify");

    // KPI cards row
    doc = g.AddSpace(doc, 12m);
    doc = g.BeginRow(doc, 10m);
    foreach (var k in kpiCards)
    {
        doc = g.BeginCell(doc, "Relative", 1m);
        doc = g.BeginBox(doc, new BoxStyle { BackgroundHex = "#eef4fb", BorderColorHex = "#cdd9e5", BorderThickness = 0.5m, Padding = 8m });
        doc = g.AddText(doc, k.Value, new TextStyle { Bold = true, FontSize = 15m, ColorHex = "#1f3b57" });
        doc = g.AddText(doc, k.Label, new TextStyle { FontSize = 8m, ColorHex = "#666666" });
        doc = g.EndBox(doc);
        doc = g.EndCell(doc);
    }
    doc = g.EndRow(doc);

    // Chart
    doc = g.AddSpace(doc, 14m);
    doc = g.AddHeading(doc, "Monthly billing", 3, new TextStyle());
    doc = g.AddSvg(doc, chartSvg, new ImageOptions { Fit = "FitWidth" });

    // Highlights
    doc = g.AddSpace(doc, 10m);
    doc = g.AddHeading(doc, "Highlights", 3, new TextStyle());
    doc = g.AddList(doc, new[] { "Retail remained the largest channel by volume.", "Overdue invoices flagged for collections review.", "Online orders continued double-digit growth." }, ordered: false, new TextStyle());

    doc = g.BeginBox(doc, new BoxStyle { BackgroundHex = "#fff8e6", BorderColorHex = "#f0a500", BorderThickness = 1m, Padding = 10m });
    doc = g.AddText(doc, "Action required: 1 review the overdue ledger before quarter close.", new TextStyle { ColorHex = "#7a5b00" });
    doc = g.EndBox(doc);

    // Large multi-page transactions table
    doc = g.AddPageBreak(doc);
    doc = g.AddHeading(doc, "Transaction detail", 2, new TextStyle { ColorHex = "#1f3b57" });
    doc = g.AddTable(doc, new TableSpec
    {
        Columns = cols,
        ColumnWidths = widths,
        HeaderBackgroundHex = "#1f3b57",
        HeaderStyle = new TextStyle { ColorHex = "#ffffff", Bold = true, FontSize = 9m },
        BodyStyle = new TextStyle { FontSize = 8m },
        ShowBorders = true,
        Rows = rows.Select(r => new TableRow { Cells = r }).ToList(),
    });

    return doc;
}

// Build once (measure build = N serialize round-trips) and capture handle size.
var swBuild = Stopwatch.StartNew();
var handle = BuildHandle();
swBuild.Stop();

// Cold render (first call — includes JIT + native warmup; license/fonts already set during Create).
var swCold = Stopwatch.StartNew();
var pdf = g.Render(handle);
swCold.Stop();

// Warm renders.
var swWarm = Stopwatch.StartNew();
for (var k = 0; k < warm; k++) pdf = g.Render(handle);
swWarm.Stop();
var warmMs = swWarm.Elapsed.TotalMilliseconds / warm;

File.WriteAllBytes(Path.Combine(outDir, "questpdf.pdf"), pdf);

// ============================ HTML (for Chromium) ============================
var html = BuildHtml();
File.WriteAllText(Path.Combine(outDir, "benchmark.html"), html);

// ---- report ----------------------------------------------------------------
Console.WriteLine("QUESTPDF_BUILD_MS=" + swBuild.Elapsed.TotalMilliseconds.ToString("F1", ci));
Console.WriteLine("QUESTPDF_HANDLE_BYTES=" + handle.Length);
Console.WriteLine("QUESTPDF_COLD_MS=" + swCold.Elapsed.TotalMilliseconds.ToString("F1", ci));
Console.WriteLine("QUESTPDF_WARM_MS=" + warmMs.ToString("F1", ci));
Console.WriteLine("QUESTPDF_PDF_BYTES=" + pdf.Length);
Console.WriteLine("ROWS=" + rowCount);

string BuildHtml()
{
    var sb = new StringBuilder();
    sb.Append("<!doctype html><html><head><meta charset='utf-8'><style>");
    sb.Append(@"
      @page { size: A4; margin: 1.6cm; }
      * { box-sizing: border-box; }
      body { font-family: Lato, Arial, sans-serif; font-size: 10pt; color: #222; margin: 0; }
      h1 { color: #1f3b57; font-size: 20pt; margin: 0 0 2px; }
      h2 { color: #1f3b57; font-size: 16pt; }
      h3 { font-size: 13pt; margin: 14px 0 6px; }
      a { color: #1a66cc; }
      .muted { color: #666; }
      .just { text-align: justify; }
      .cards { display: flex; gap: 10px; margin: 12px 0; }
      .card { flex: 1; background: #eef4fb; border: 0.5px solid #cdd9e5; padding: 8px; }
      .card .v { font-weight: 700; font-size: 15pt; color: #1f3b57; }
      .card .l { font-size: 8pt; color: #666; }
      .callout { background: #fff8e6; border: 1px solid #f0a500; padding: 10px; color: #7a5b00; margin: 8px 0; }
      ul { margin: 4px 0; padding-left: 20px; }
      table { border-collapse: collapse; width: 100%; font-size: 8pt; }
      thead { display: table-header-group; }
      th { background: #1f3b57; color: #fff; font-weight: 700; font-size: 9pt; text-align: left; padding: 5px 6px; border: 0.5px solid #345; }
      td { padding: 4px 6px; border: 0.5px solid #dfe6ee; }
      .watermark { position: fixed; top: 45%; left: 0; right: 0; text-align: center;
                   font-size: 90pt; font-weight: 700; color: #d8dee6;
                   transform: rotate(-45deg); z-index: -1; }
      .pagebreak { page-break-before: always; }
    ");
    sb.Append("</style></head><body>");
    sb.Append("<div class='watermark'>CONFIDENTIAL</div>");
    sb.Append(logoSvg);
    sb.Append("<h1>Q2 2026 Financial &amp; Operations Report</h1>");
    sb.Append("<div class='muted'>Prepared 21 June 2026 · Finance &amp; Operations</div>");
    sb.Append("<p class='just'>This report summarises <b>" + rowCount.ToString("N0", ci) + " transactions</b> across all channels. Totals are <i style='color:#cc2b2b'>preliminary</i> and subject to audit. See <a href='https://www.questpdf.com'>questpdf.com</a>.</p>");

    sb.Append("<div class='cards'>");
    foreach (var k in kpiCards)
        sb.Append($"<div class='card'><div class='v'>{k.Value}</div><div class='l'>{k.Label}</div></div>");
    sb.Append("</div>");

    sb.Append("<h3>Monthly billing</h3>");
    sb.Append(chartSvg);

    sb.Append("<h3>Highlights</h3><ul><li>Retail remained the largest channel by volume.</li><li>Overdue invoices flagged for collections review.</li><li>Online orders continued double-digit growth.</li></ul>");
    sb.Append("<div class='callout'>Action required: review the overdue ledger before quarter close.</div>");

    sb.Append("<div class='pagebreak'></div><h2>Transaction detail</h2>");
    sb.Append("<table><thead><tr>");
    foreach (var c in cols) sb.Append("<th>" + System.Net.WebUtility.HtmlEncode(c) + "</th>");
    sb.Append("</tr></thead><tbody>");
    foreach (var r in rows)
    {
        sb.Append("<tr>");
        foreach (var cell in r) sb.Append("<td>" + System.Net.WebUtility.HtmlEncode(cell) + "</td>");
        sb.Append("</tr>");
    }
    sb.Append("</tbody></table></body></html>");
    return sb.ToString();
}

static string BuildChartSvg(double[] monthly)
{
    var max = Math.Max(1, monthly.Max());
    string[] m = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    var sb = new StringBuilder();
    int w = 540, h = 200, pad = 24, bw = (w - pad * 2) / 12;
    sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{w}' height='{h}'>");
    sb.Append($"<rect width='{w}' height='{h}' fill='#fafbfc'/>");
    for (var i = 0; i < 12; i++)
    {
        var bh = (int)((monthly[i] / max) * (h - pad * 2));
        var x = pad + i * bw + 3;
        var y = h - pad - bh;
        sb.Append($"<rect x='{x}' y='{y}' width='{bw - 6}' height='{bh}' fill='#1f3b57'/>");
        sb.Append($"<text x='{x + (bw - 6) / 2}' y='{h - pad + 12}' font-size='8' text-anchor='middle' fill='#666' font-family='sans-serif'>{m[i]}</text>");
    }
    sb.Append("</svg>");
    return sb.ToString();
}
