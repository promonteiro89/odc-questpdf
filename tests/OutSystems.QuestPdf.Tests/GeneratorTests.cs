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

    private static byte[] Render(IEnumerable<Block> content, DocumentOptions? options = null)
        => G.Render(options ?? new DocumentOptions { PageSize = "A4" }, content);

    private static (int Pages, string Text) Inspect(byte[] pdf)
    {
        Assert.True(pdf.Length > 800, "PDF is implausibly small");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        using var doc = PdfDocument.Open(pdf);
        var text = string.Join("\n", doc.GetPages().Select(p => p.Text));
        return (doc.NumberOfPages, text);
    }

    private static void AssertThrowsContaining(string fragment, Action act)
    {
        var ex = Assert.ThrowsAny<Exception>(act);
        Assert.Contains(fragment, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- rendering ----------------------------------------------------------

    [Fact]
    public void Render_SimpleDocument_ProducesValidPdf()
    {
        var (pages, text) = Inspect(Render(new List<Block>
        {
            new() { Type = "Heading", Text = "HelloHeading", Level = 1 },
            new() { Type = "Text", Text = "BodyContent" },
        }));
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
        var (pages, text) = Inspect(Render(new List<Block>
        {
            new() { Type = "Table", Table = new TableSpec { Columns = new[] { "Id", "Item", "N" }, Rows = rows, ShowBorders = true } },
        }));
        Assert.True(pages >= 4, $"expected multi-page pagination, got {pages}");
        Assert.Contains("ROW0", text);
        Assert.Contains("ROW150", text);
    }

    [Fact]
    public void RichText_List_Box_Render()
    {
        var (_, text) = Inspect(Render(new List<Block>
        {
            new() { Type = "RichText", Alignment = "Left", Spans = new[]
            {
                new TextSpan { Text = "Plain " },
                new TextSpan { Text = "boldrun", Style = new TextStyle { Bold = true } },
                new TextSpan { Text = " link", Style = new TextStyle { ColorHex = "#1a66cc" }, Hyperlink = "https://questpdf.com" },
            } },
            new() { Type = "List", Ordered = true, Items = new[] { "ItemAlpha", "ItemBeta" } },
            new() { Type = "BeginBox", Box = new BoxStyle { BackgroundHex = "#eef4fb", BorderThickness = 1m, Padding = 6m } },
            new() { Type = "Text", Text = "InBox" },
            new() { Type = "EndBox" },
        }));
        Assert.Contains("boldrun", text);
        Assert.Contains("ItemAlpha", text);
        Assert.Contains("InBox", text);
    }

    [Fact]
    public void NestedRowsAndCells_Render()
    {
        var (_, text) = Inspect(Render(new List<Block>
        {
            new() { Type = "BeginRow", Spacing = 10m },
            new() { Type = "BeginCell", WidthType = "Relative", Width = 1m },
            new() { Type = "Text", Text = "LeftCell" },
            new() { Type = "EndCell" },
            new() { Type = "BeginCell", WidthType = "Constant", Width = 120m },
            new() { Type = "Text", Text = "RightCell", Style = new TextStyle { Alignment = "Right" } },
            new() { Type = "EndCell" },
            new() { Type = "EndRow" },
        }));
        Assert.Contains("LeftCell", text);
        Assert.Contains("RightCell", text);
    }

    [Fact]
    public void TableOfContents_Sections_And_SectionLink_Render()
    {
        var (pages, text) = Inspect(Render(new List<Block>
        {
            new() { Type = "Toc", Toc = new[]
            {
                new TocEntry { Label = "ChapterOne", SectionName = "ch1" },
                new TocEntry { Label = "ChapterTwo", SectionName = "ch2", Indent = 1 },
            } },
            new() { Type = "RichText", Spans = new[] { new TextSpan { Text = "GoTo", SectionLink = "ch2" } } },
            new() { Type = "PageBreak" },
            new() { Type = "BeginSection", SectionName = "ch1" },
            new() { Type = "Heading", Text = "ChapterOne", Level = 1 },
            new() { Type = "Text", Text = "ChapterOneBody" },
            new() { Type = "EndSection" },
            new() { Type = "PageBreak" },
            new() { Type = "BeginSection", SectionName = "ch2" },
            new() { Type = "Heading", Text = "ChapterTwo", Level = 1 },
            new() { Type = "Text", Text = "ChapterTwoBody" },
            new() { Type = "EndSection" },
        }));
        Assert.True(pages >= 3);
        Assert.Contains("ChapterOneBody", text);
        Assert.Contains("ChapterTwoBody", text);
    }

    [Fact]
    public void HeaderFooterWatermark_Render()
    {
        var options = new DocumentOptions
        {
            PageSize = "A4",
            Header = new HeaderFooter { Text = "HeaderHere" },
            Footer = new HeaderFooter { Text = "FooterHere", ShowPageNumbers = true },
            Watermark = new Watermark { Text = "WMARK" },
        };
        var (_, text) = Inspect(Render(new List<Block> { new() { Type = "Text", Text = "Body" } }, options));
        Assert.Contains("HeaderHere", text);
        Assert.Contains("FooterHere", text);
    }

    [Fact]
    public void Metadata_EmbedsTitleAndAuthor()
    {
        var options = new DocumentOptions { PageSize = "A4", Title = "MetaTitle", Author = "MetaAuthor", Subject = "MetaSubject" };
        using var pdf = PdfDocument.Open(Render(new List<Block> { new() { Type = "Text", Text = "x" } }, options));
        Assert.Equal("MetaTitle", pdf.Information.Title);
        Assert.Equal("MetaAuthor", pdf.Information.Author);
    }

    // ---- validation & clear errors ------------------------------------------

    [Fact]
    public void InvalidHexColor_Throws()
        => AssertThrowsContaining("hex color", () => Render(new List<Block>
        {
            new() { Type = "Text", Text = "x", Style = new TextStyle { ColorHex = "red" } },
        }));

    [Fact]
    public void InvalidImageBytes_Throws()
        => AssertThrowsContaining("image", () => Render(new List<Block>
        {
            new() { Type = "Image", Image = new byte[] { 1, 2, 3, 4, 5 } },
        }));

    [Fact]
    public void UnclosedContainer_Throws()
        => AssertThrowsContaining("unclosed", () => Render(new List<Block>
        {
            new() { Type = "BeginRow" },
            new() { Type = "Text", Text = "x" },
        }));

    [Fact]
    public void EndWithoutBegin_Throws()
        => AssertThrowsContaining("no matching", () => Render(new List<Block> { new() { Type = "EndRow" } }));

    [Fact]
    public void UnknownBlockType_Throws()
        => AssertThrowsContaining("unknown block type", () => Render(new List<Block> { new() { Type = "Nonsense" } }));

    [Fact]
    public void DuplicateSectionName_Throws()
        => AssertThrowsContaining("more than once", () => Render(new List<Block>
        {
            new() { Type = "BeginSection", SectionName = "dup" },
            new() { Type = "EndSection" },
            new() { Type = "BeginSection", SectionName = "dup" },
            new() { Type = "EndSection" },
        }));

    [Fact]
    public void FontRef_BadUrl_Throws()
        => AssertThrowsContaining("URL", () => G.Render(
            new DocumentOptions { Fonts = new[] { new FontRef { Name = "X", Url = "not-a-url" } } },
            new List<Block> { new() { Type = "Text", Text = "x" } }));

    // ---- fonts (bytes + URL) ------------------------------------------------

    [Fact]
    public void Font_FromBytes_Renders()
    {
        var path = FindTtf();
        if (path is null) return;
        var options = new DocumentOptions { FontFamily = "BrandBytes", FontSize = 12m, Fonts = new[] { new FontRef { Name = "BrandBytes", Bytes = File.ReadAllBytes(path) } } };
        var (_, text) = Inspect(Render(new List<Block> { new() { Type = "Text", Text = "BytesFontText", Style = new TextStyle { FontFamily = "BrandBytes" } } }, options));
        Assert.Contains("BytesFontText", text);
    }

    [Fact]
    public async Task Font_FromUrl_FetchesAndRenders()
    {
        var path = FindTtf();
        if (path is null) return;
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

        var options = new DocumentOptions { FontFamily = "UrlBrand", Fonts = new[] { new FontRef { Name = "UrlBrand", Url = $"http://localhost:{port}/brand.ttf" } } };
        var pdf = Render(new List<Block> { new() { Type = "Text", Text = "UrlFontText", Style = new TextStyle { FontFamily = "UrlBrand" } } }, options);

        await Task.WhenAny(server, Task.Delay(3000));
        listener.Stop();
        var (_, text) = Inspect(pdf);
        Assert.Contains("UrlFontText", text);
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

        var result = G.RenderAndStore(
            new DocumentOptions { PageSize = "A4" },
            new List<Block> { new() { Type = "Text", Text = "StoredPdf" } },
            $"http://localhost:{port}/upload", "tok123");

        await server.WaitAsync(TimeSpan.FromSeconds(15));
        listener.Stop();

        Assert.True(result.Success, result.Error);
        Assert.Equal(200, result.StatusCode);
        Assert.True(result.SizeBytes > 500);
        Assert.NotNull(received);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(received!, 0, 4));
        Assert.Equal("Bearer tok123", auth);
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

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
