using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using OutSystems.ExternalLibraries.SDK;
using OutSystems.QuestPdf.Internal;
using OutSystems.QuestPdf.Models;
using OutSystems.QuestPdf.Rendering;
using LicenseType = QuestPDF.Infrastructure.LicenseType;

namespace OutSystems.QuestPdf;

/// <summary>
/// The single public implementation of <see cref="IQuestPdfGenerator"/>. ODC
/// requires exactly one [OSInterface], implemented by one public class with a
/// public parameterless constructor.
///
/// Every builder action deserializes the document handle, mutates it, and
/// re-serializes — a stateless byte[] round-trip.
/// </summary>
public sealed class QuestPdfGenerator : IQuestPdfGenerator
{
    // ============================ LICENSE — READ THIS ============================
    // QuestPDF Community (free) is valid only for organisations under USD 1,000,000
    // annual gross revenue. Above that, set Professional/Enterprise and procure a
    // license. The enum is honour-system (not runtime-enforced); this is a legal
    // decision. (Kept as Community per project decision.)
    private const LicenseType LicenseTier = LicenseType.Community;
    // ============================================================================

    private static readonly object InitGate = new();
    private static bool _initialized;

    private readonly ILogger? _logger;

    /// <summary>Parameterless constructor (used locally / in tests; ODC also accepts it).</summary>
    public QuestPdfGenerator() : this(null) { }

    /// <summary>
    /// The ODC runtime injects an ILogger here (Custom Code Logging &amp; Tracing).
    /// Logging is null-safe, so this also works outside ODC.
    /// </summary>
    public QuestPdfGenerator(ILogger? logger)
    {
        _logger = logger;
        EnsureInitialized();
    }

    internal static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (InitGate)
        {
            if (_initialized) return;
            QuestPDF.Settings.License = LicenseTier;
            QuestPDF.Settings.UseEnvironmentFonts = false;
            FontBootstrapper.RegisterEmbeddedFonts();
            _initialized = true;
        }
    }

    // ---- lifecycle ----------------------------------------------------------

    public byte[] Create(DocumentOptions options)
    {
        EnsureInitialized();
        return new DocumentSpec { Options = OptionsSpec.From(options) }.ToBytes();
    }

    public byte[] Render(byte[] document)
    {
        EnsureInitialized();
        return RenderInternal(DocumentSpec.FromBytes(document), nameof(Render));
    }

    public byte[] RenderJson(string documentJson)
    {
        EnsureInitialized();
        return RenderInternal(DocumentSpec.FromText(documentJson), nameof(RenderJson));
    }

    public UploadResult RenderAndStore(byte[] document, string callbackUrl, string authToken)
    {
        EnsureInitialized();
        var pdf = RenderInternal(DocumentSpec.FromBytes(document), nameof(RenderAndStore));
        var result = PdfDelivery.Post(pdf, callbackUrl, authToken, "document.pdf");
        if (result.Success)
            _logger?.LogInformation("QuestPdf stored PDF: {Bytes} bytes, HTTP {Status}", result.SizeBytes, result.StatusCode);
        else
            _logger?.LogWarning("QuestPdf store failed: HTTP {Status} {Error}", result.StatusCode, result.Error);
        return result;
    }

    private byte[] RenderInternal(DocumentSpec spec, string operation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var pdf = new PdfComposer(spec).Render();
            sw.Stop();
            _logger?.LogInformation("QuestPdf {Operation} succeeded: {Bytes} bytes in {ElapsedMs} ms", operation, pdf.Length, sw.ElapsedMilliseconds);
            return pdf;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogError(ex, "QuestPdf {Operation} failed after {ElapsedMs} ms: {Message}", operation, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    // ---- content primitives -------------------------------------------------

    public byte[] AddHeading(byte[] document, string text, int level, TextStyle style) =>
        Mutate(document, s => s.Append(new DocNode
        {
            Type = "Heading",
            Text = text,
            Level = level <= 0 ? 1 : level,
            Style = StyleSpec.From(style),
        }));

    public byte[] AddText(byte[] document, string text, TextStyle style) =>
        Mutate(document, s => s.Append(new DocNode { Type = "Text", Text = text, Style = StyleSpec.From(style) }));

    public byte[] AddRichText(byte[] document, IEnumerable<TextSpan> spans, string alignment) =>
        Mutate(document, s => s.Append(new DocNode
        {
            Type = "RichText",
            Align = string.IsNullOrWhiteSpace(alignment) ? null : alignment.Trim(),
            Spans = (spans ?? Enumerable.Empty<TextSpan>()).Select(SpanData.From).ToList(),
        }));

    public byte[] AddList(byte[] document, IEnumerable<string> items, bool ordered, TextStyle style) =>
        Mutate(document, s => s.Append(new DocNode
        {
            Type = "List",
            Ordered = ordered,
            Style = StyleSpec.From(style),
            Items = (items ?? Enumerable.Empty<string>()).ToList(),
        }));

    public byte[] AddSvg(byte[] document, string svg, ImageOptions options) =>
        Mutate(document, s => s.Append(new DocNode
        {
            Type = "Svg",
            Svg = svg,
            Fit = string.IsNullOrWhiteSpace(options.Fit) ? null : options.Fit.Trim(),
            MaxWidth = (double)options.MaxWidthPoints,
            Align = string.IsNullOrWhiteSpace(options.Alignment) ? null : options.Alignment.Trim(),
        }));

    public byte[] AddImage(byte[] document, byte[] image, ImageOptions options)
    {
        Validate.ImageBytes(image, "AddImage.image");
        return Mutate(document, s => s.Append(new DocNode
        {
            Type = "Image",
            Image = image,
            Fit = string.IsNullOrWhiteSpace(options.Fit) ? null : options.Fit.Trim(),
            MaxWidth = (double)options.MaxWidthPoints,
            Align = string.IsNullOrWhiteSpace(options.Alignment) ? null : options.Alignment.Trim(),
        }));
    }

    public byte[] AddTable(byte[] document, TableSpec table) =>
        Mutate(document, s => s.Append(new DocNode { Type = "Table", Table = TableSpecData.From(table) }));

    public byte[] AddDivider(byte[] document, string colorHex, decimal thickness) =>
        Mutate(document, s => s.Append(new DocNode
        {
            Type = "Divider",
            ColorHex = string.IsNullOrWhiteSpace(colorHex) ? null : colorHex.Trim(),
            Number = (double)thickness,
        }));

    public byte[] AddSpace(byte[] document, decimal heightPoints) =>
        Mutate(document, s => s.Append(new DocNode { Type = "Space", Number = (double)heightPoints }));

    public byte[] AddPageBreak(byte[] document) =>
        Mutate(document, s => s.Append(new DocNode { Type = "PageBreak" }));

    // ---- layout containers --------------------------------------------------

    public byte[] BeginRow(byte[] document, decimal spacing) =>
        Mutate(document, s => s.Open(new DocNode { Type = "Row", Number = (double)spacing }));

    public byte[] EndRow(byte[] document) => Mutate(document, s => s.Close("Row"));

    public byte[] BeginColumn(byte[] document, decimal spacing) =>
        Mutate(document, s => s.Open(new DocNode { Type = "Column", Number = (double)spacing }));

    public byte[] EndColumn(byte[] document) => Mutate(document, s => s.Close("Column"));

    public byte[] BeginCell(byte[] document, string widthType, decimal width) =>
        Mutate(document, s => s.Open(new DocNode
        {
            Type = "Cell",
            WidthType = string.Equals(widthType?.Trim(), "Constant", System.StringComparison.OrdinalIgnoreCase) ? "Constant" : "Relative",
            Width = (double)width,
        }));

    public byte[] EndCell(byte[] document) => Mutate(document, s => s.Close("Cell"));

    public byte[] BeginBox(byte[] document, BoxStyle style) =>
        Mutate(document, s => s.Open(new DocNode { Type = "Box", Box = BoxStyleData.From(style) }));

    public byte[] EndBox(byte[] document) => Mutate(document, s => s.Close("Box"));

    // ---- header / footer / watermark ----------------------------------------

    public byte[] SetHeader(byte[] document, string text, TextStyle style) =>
        Mutate(document, s => s.Header = new HeaderFooterSpec { Text = text, Style = StyleSpec.From(style) });

    public byte[] SetFooter(byte[] document, string text, TextStyle style, bool showPageNumbers) =>
        Mutate(document, s => s.Footer = new HeaderFooterSpec { Text = text, Style = StyleSpec.From(style), ShowPageNumbers = showPageNumbers });

    public byte[] SetWatermark(byte[] document, string text, TextStyle style) =>
        Mutate(document, s => s.Watermark = new WatermarkSpec { Text = text, Style = StyleSpec.From(style) });

    public byte[] SetMetadata(byte[] document, string title, string author, string subject) =>
        Mutate(document, s =>
        {
            s.Title = string.IsNullOrWhiteSpace(title) ? null : title;
            s.Author = string.IsNullOrWhiteSpace(author) ? null : author;
            s.Subject = string.IsNullOrWhiteSpace(subject) ? null : subject;
        });

    public byte[] RegisterFont(byte[] document, string fontName, byte[] fontBytes) =>
        Mutate(document, s =>
        {
            if (string.IsNullOrWhiteSpace(fontName))
                throw new ArgumentException("RegisterFont requires a non-empty fontName.");
            if (fontBytes is not { Length: > 0 })
                throw new ArgumentException($"RegisterFont(\"{fontName}\") requires non-empty TTF/OTF font bytes.");
            s.Fonts ??= new List<FontData>();
            s.Fonts.Add(new FontData { Name = fontName.Trim(), Data = fontBytes });
        });

    public byte[] RegisterFontFromUrl(byte[] document, string fontName, string fontUrl) =>
        Mutate(document, s =>
        {
            if (string.IsNullOrWhiteSpace(fontName))
                throw new ArgumentException("RegisterFontFromUrl requires a non-empty fontName.");
            if (!Uri.TryCreate(fontUrl?.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException($"RegisterFontFromUrl(\"{fontName}\") requires an absolute http(s) URL, got \"{fontUrl}\".");
            s.Fonts ??= new List<FontData>();
            s.Fonts.Add(new FontData { Name = fontName.Trim(), Url = uri.ToString() });
        });

    public byte[] BeginSection(byte[] document, string name) =>
        Mutate(document, s =>
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("BeginSection requires a non-empty name.");
            if (s.SectionExists(name.Trim()))
                throw new ArgumentException($"Section name \"{name}\" is already used. Section names must be unique so links and the table of contents resolve correctly.");
            s.Open(new DocNode { Type = "Section", Name = name.Trim() });
        });

    public byte[] EndSection(byte[] document) => Mutate(document, s => s.Close("Section"));

    public byte[] AddTableOfContents(byte[] document, IEnumerable<TocEntry> entries, TextStyle style) =>
        Mutate(document, s => s.Append(new DocNode
        {
            Type = "Toc",
            Style = StyleSpec.From(style),
            Toc = (entries ?? Enumerable.Empty<TocEntry>()).Select(TocEntryData.From).ToList(),
        }));

    // ---- helper -------------------------------------------------------------

    private byte[] Mutate(byte[] document, Action<DocumentSpec> mutation)
    {
        var spec = DocumentSpec.FromBytes(document);
        mutation(spec);
        var bytes = spec.ToBytes();
        if (bytes.Length > 4_500_000)
            _logger?.LogWarning(
                "QuestPdf document handle is {Bytes} bytes, approaching the 5.5 MB ODC payload limit. Consider RenderJson (single call), or add large images later / use fewer of them.",
                bytes.Length);
        return bytes;
    }
}
