using System.Net;
using System.Net.Sockets;
using System.Text;
using OutSystems.QuestPdf;
using OutSystems.QuestPdf.Models;
using UglyToad.PdfPig;
using Xunit;

namespace OutSystems.QuestPdf.Tests;

public class GeneratorTests
{
    private static readonly QuestPdfGenerator G = new();

    private static (int Pages, string Text) Inspect(byte[] pdf)
    {
        Assert.True(pdf.Length > 800, "PDF is implausibly small");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        using var doc = PdfDocument.Open(pdf);
        var text = string.Join("\n", doc.GetPages().Select(p => p.Text));
        return (doc.NumberOfPages, text);
    }

    // ---- rendering ----------------------------------------------------------

    [Fact]
    public void Render_SimpleDocument_ProducesValidPdf()
    {
        var doc = G.Create(new DocumentOptions { PageSize = "A4" });
        doc = G.AddHeading(doc, "HelloHeading", 1, new TextStyle());
        doc = G.AddText(doc, "BodyContent", new TextStyle());
        var (pages, text) = Inspect(G.Render(doc));

        Assert.Equal(1, pages);
        Assert.Contains("HelloHeading", text);
        Assert.Contains("BodyContent", text);
    }

    [Fact]
    public void Render_LargeTable_PaginatesAcrossPages()
    {
        var rows = Enumerable.Range(0, 200)
            .Select(i => new TableRow { Cells = new[] { $"ROW{i}", "Widget", i.ToString() } })
            .ToList();
        var doc = G.Create(new DocumentOptions());
        doc = G.AddTable(doc, new TableSpec
        {
            Columns = new[] { "Id", "Item", "N" },
            Rows = rows,
            ShowBorders = true,
        });
        var (pages, text) = Inspect(G.Render(doc));

        Assert.True(pages >= 4, $"expected multi-page pagination, got {pages}");
        Assert.Contains("ROW0", text);
        Assert.Contains("ROW150", text);
    }

    [Fact]
    public void RichText_And_AllPrimitives_Render()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.SetHeader(doc, "Hdr", new TextStyle());
        doc = G.SetFooter(doc, "Ftr", new TextStyle(), showPageNumbers: true);
        doc = G.SetWatermark(doc, "WMARK", new TextStyle());
        doc = G.AddRichText(doc, new[]
        {
            new TextSpan { Text = "Plain " },
            new TextSpan { Text = "boldrun", Style = new TextStyle { Bold = true } },
            new TextSpan { Text = " link", Style = new TextStyle { ColorHex = "#1a66cc" }, Hyperlink = "https://questpdf.com" },
        }, "Left");
        doc = G.AddList(doc, new[] { "ItemAlpha", "ItemBeta" }, ordered: true, new TextStyle());
        doc = G.BeginBox(doc, new BoxStyle { BackgroundHex = "#eef4fb", BorderThickness = 1m, Padding = 6m });
        doc = G.AddText(doc, "InBox", new TextStyle());
        doc = G.EndBox(doc);
        var (_, text) = Inspect(G.Render(doc));

        Assert.Contains("boldrun", text);
        Assert.Contains("ItemAlpha", text);
        Assert.Contains("InBox", text);
    }

    [Fact]
    public void NestedRowsAndCells_Render()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.BeginRow(doc, 10m);
        doc = G.BeginCell(doc, "Relative", 1m);
        doc = G.AddText(doc, "LeftCell", new TextStyle());
        doc = G.EndCell(doc);
        doc = G.BeginCell(doc, "Constant", 120m);
        doc = G.AddText(doc, "RightCell", new TextStyle { Alignment = "Right" });
        doc = G.EndCell(doc);
        doc = G.EndRow(doc);
        var (_, text) = Inspect(G.Render(doc));

        Assert.Contains("LeftCell", text);
        Assert.Contains("RightCell", text);
    }

    // ---- single-call JSON path (freedom) ------------------------------------

    [Fact]
    public void RenderJson_RendersFullTreeInOneCall()
    {
        const string json = """
        {
          "options": { "pageSize": "A4", "marginCm": 2 },
          "title": "JsonTitle",
          "root": { "type": "Column", "number": 8, "children": [
            { "type": "Heading", "text": "JsonHeading", "level": 1 },
            { "type": "Text", "text": "OneCallBody" },
            { "type": "Row", "number": 10, "children": [
              { "type": "Cell", "widthType": "Relative", "width": 1, "children": [ { "type": "Text", "text": "JsonLeft" } ] },
              { "type": "Cell", "widthType": "Relative", "width": 1, "children": [ { "type": "Text", "text": "JsonRight" } ] }
            ] }
          ] }
        }
        """;
        var (pages, text) = Inspect(G.RenderJson(json));

        Assert.Equal(1, pages);
        Assert.Contains("JsonHeading", text);
        Assert.Contains("OneCallBody", text);
        Assert.Contains("JsonLeft", text);
        Assert.Contains("JsonRight", text);
    }

    [Fact]
    public void RenderJson_EquivalentToChainedBuilder()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.AddHeading(doc, "SameDoc", 1, new TextStyle());
        doc = G.AddText(doc, "SameBody", new TextStyle());

        var fromBuilder = Inspect(G.Render(doc));
        // The handle IS the JSON spec — feed it straight into the single-call path.
        var handleJson = Encoding.UTF8.GetString(doc);
        var fromJson = Inspect(G.RenderJson(handleJson));

        Assert.Equal(fromBuilder.Pages, fromJson.Pages);
        Assert.Equal(fromBuilder.Text, fromJson.Text);
    }

    // ---- navigation: sections + TOC + section links -------------------------

    [Fact]
    public void TableOfContents_AndSections_Render()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.AddHeading(doc, "Contents", 1, new TextStyle());
        doc = G.AddTableOfContents(doc, new[]
        {
            new TocEntry { Label = "ChapterOne", SectionName = "ch1" },
            new TocEntry { Label = "ChapterTwo", SectionName = "ch2", Indent = 1 },
        }, new TextStyle());
        doc = G.AddPageBreak(doc);
        doc = G.BeginSection(doc, "ch1");
        doc = G.AddHeading(doc, "ChapterOne", 1, new TextStyle());
        doc = G.AddText(doc, "ChapterOneBody", new TextStyle());
        doc = G.EndSection(doc);
        doc = G.AddPageBreak(doc);
        doc = G.BeginSection(doc, "ch2");
        doc = G.AddHeading(doc, "ChapterTwo", 1, new TextStyle());
        doc = G.AddText(doc, "ChapterTwoBody", new TextStyle());
        doc = G.EndSection(doc);

        var (pages, text) = Inspect(G.Render(doc));
        Assert.True(pages >= 3, $"expected sections on separate pages, got {pages}");
        Assert.Contains("ChapterOneBody", text);
        Assert.Contains("ChapterTwoBody", text);
    }

    [Fact]
    public void SectionLink_InRichText_Renders()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.AddRichText(doc, new[]
        {
            new TextSpan { Text = "JumpTo " },
            new TextSpan { Text = "TheAppendix", Style = new TextStyle { ColorHex = "#1a66cc" }, SectionLink = "appendix" },
        }, "Left");
        doc = G.AddPageBreak(doc);
        doc = G.BeginSection(doc, "appendix");
        doc = G.AddHeading(doc, "TheAppendix", 1, new TextStyle());
        doc = G.EndSection(doc);

        var (_, text) = Inspect(G.Render(doc));
        Assert.Contains("JumpTo", text);
        Assert.Contains("TheAppendix", text);
    }

    [Fact]
    public void UnclosedSection_ThrowsAtRender()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.BeginSection(doc, "x");
        doc = G.AddText(doc, "y", new TextStyle());
        var ex = Assert.Throws<InvalidOperationException>(() => G.Render(doc));
        Assert.Contains("unclosed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- runtime fonts ------------------------------------------------------

    [Fact]
    public void RegisterFont_EmptyBytes_Throws()
    {
        var doc = G.Create(new DocumentOptions());
        Assert.Throws<ArgumentException>(() => G.RegisterFont(doc, "MyFont", Array.Empty<byte>()));
    }

    [Fact]
    public void RegisterFont_RealFont_RendersWithThatFamily()
    {
        var path = FindTtf();
        if (path is null) return; // no usable TTF on this machine/CI — skip the render assertion
        var bytes = File.ReadAllBytes(path);

        var doc = G.Create(new DocumentOptions { FontFamily = "BrandFont", FontSize = 12m });
        doc = G.RegisterFont(doc, "BrandFont", bytes);
        doc = G.AddText(doc, "RuntimeFontText", new TextStyle { FontFamily = "BrandFont" });

        var (_, text) = Inspect(G.Render(doc));
        Assert.Contains("RuntimeFontText", text);
    }

    [Fact]
    public void RegisterFontFromUrl_BadUrl_Throws()
    {
        var doc = G.Create(new DocumentOptions());
        Assert.Throws<ArgumentException>(() => G.RegisterFontFromUrl(doc, "X", ""));
        Assert.Throws<ArgumentException>(() => G.RegisterFontFromUrl(doc, "X", "not-a-url"));
    }

    [Fact]
    public async Task RegisterFontFromUrl_FetchesAndRenders()
    {
        var path = FindTtf();
        if (path is null) return; // no usable TTF on this machine/CI — skip
        var fontBytes = File.ReadAllBytes(path);

        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        var server = Task.Run(() =>
        {
            var ctx = listener.GetContext();
            ctx.Response.ContentType = "font/ttf";
            ctx.Response.OutputStream.Write(fontBytes, 0, fontBytes.Length);
            ctx.Response.Close();
        });

        var doc = G.Create(new DocumentOptions { FontFamily = "UrlBrand", FontSize = 12m });
        doc = G.RegisterFontFromUrl(doc, "UrlBrand", $"http://localhost:{port}/brand.ttf");
        doc = G.AddText(doc, "UrlFontText", new TextStyle { FontFamily = "UrlBrand" });
        var pdf = G.Render(doc); // triggers the server-side download

        await Task.WhenAny(server, Task.Delay(3000)); // don't deadlock if the font was already cached
        listener.Stop();

        var (_, text) = Inspect(pdf);
        Assert.Contains("UrlFontText", text);
    }

    private static string? FindTtf()
    {
        string[] candidates =
        {
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "/System/Library/Fonts/Supplemental/Georgia.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/Library/Fonts/Arial.ttf",
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    // ---- metadata -----------------------------------------------------------

    [Fact]
    public void SetMetadata_EmbedsTitleAndAuthor()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.SetMetadata(doc, "MetaTitle", "MetaAuthor", "MetaSubject");
        doc = G.AddText(doc, "x", new TextStyle());
        using var pdf = PdfDocument.Open(G.Render(doc));

        Assert.Equal("MetaTitle", pdf.Information.Title);
        Assert.Equal("MetaAuthor", pdf.Information.Author);
    }

    // ---- validation & clear errors ------------------------------------------

    [Fact]
    public void InvalidHexColor_ThrowsAtAddCall()
    {
        var doc = G.Create(new DocumentOptions());
        var ex = Assert.Throws<ArgumentException>(() => G.AddText(doc, "x", new TextStyle { ColorHex = "red" }));
        Assert.Contains("hex color", ex.Message);
    }

    [Fact]
    public void UnclosedContainer_ThrowsAtRender()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.BeginRow(doc, 0m);
        doc = G.AddText(doc, "x", new TextStyle());
        var ex = Assert.Throws<InvalidOperationException>(() => G.Render(doc));
        Assert.Contains("unclosed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EndWithoutBegin_Throws()
    {
        var doc = G.Create(new DocumentOptions());
        Assert.Throws<InvalidOperationException>(() => G.EndRow(doc));
    }

    [Fact]
    public void EmptyHandle_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => G.Render(Array.Empty<byte>()));
    }

    [Fact]
    public void AddImage_InvalidBytes_Throws()
    {
        var doc = G.Create(new DocumentOptions());
        Assert.Throws<ArgumentException>(() => G.AddImage(doc, Array.Empty<byte>(), new ImageOptions()));
        Assert.Throws<ArgumentException>(() => G.AddImage(doc, new byte[] { 1, 2, 3, 4, 5 }, new ImageOptions()));
    }

    [Fact]
    public void DuplicateSectionName_Throws()
    {
        var doc = G.Create(new DocumentOptions());
        doc = G.BeginSection(doc, "dup");
        doc = G.AddText(doc, "x", new TextStyle());
        doc = G.EndSection(doc);
        Assert.Throws<ArgumentException>(() => G.BeginSection(doc, "dup"));
    }

    // ---- RenderAndStore (HTTP path verified end-to-end) ---------------------

    [Fact]
    public async Task RenderAndStore_PostsPdfToCallback()
    {
        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        byte[]? received = null;
        string? auth = null;
        var server = Task.Run(() =>
        {
            var ctx = listener.GetContext();
            auth = ctx.Request.Headers["Authorization"];
            using var ms = new MemoryStream();
            ctx.Request.InputStream.CopyTo(ms);
            received = ms.ToArray();
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        });

        var doc = G.Create(new DocumentOptions());
        doc = G.AddText(doc, "StoredPdf", new TextStyle());
        var result = G.RenderAndStore(doc, $"http://localhost:{port}/upload", "tok123");

        await server.WaitAsync(TimeSpan.FromSeconds(15));
        listener.Stop();

        Assert.True(result.Success, result.Error);
        Assert.Equal(200, result.StatusCode);
        Assert.True(result.SizeBytes > 500);
        Assert.NotNull(received);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(received!, 0, 4));
        Assert.Equal("Bearer tok123", auth);
    }

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
