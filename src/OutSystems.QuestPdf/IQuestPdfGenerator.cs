using OutSystems.ExternalLibraries.SDK;
using OutSystems.QuestPdf.Models;

namespace OutSystems.QuestPdf;

/// <summary>
/// A builder-style PDF API for ODC — no templates. You compose a document from
/// primitives, passing a Binary Data handle in and out of every action:
///
///   doc = Create(options)
///   doc = AddHeading(doc, "Invoice", 1, style)
///   doc = BeginRow(doc, 10) ... AddText(doc, ...) ... EndRow(doc)
///   doc = AddTable(doc, table)
///   pdf = Render(doc)
///
/// The "document" passed in and out of every action is an opaque Binary Data
/// handle (a serialized document spec, NOT a PDF). Render is what produces the PDF.
/// </summary>
[OSInterface(
    Name = "QuestPDF",
    Description = "Builder-style PDF generation. Compose a document from primitives, then Render it to Binary Data.",
    IconResourceName = IconNames.App)]
public interface IQuestPdfGenerator
{
    // ---- lifecycle ----------------------------------------------------------

    [OSAction(IconResourceName = IconNames.Action, Description ="Start a new, empty document. Returns the document handle to pass into the Add/Begin actions.", ReturnName = "Document")]
    byte[] Create([OSParameter(Description = "Page size, orientation, margins and default font.")] DocumentOptions options);

    [OSAction(IconResourceName = IconNames.Action, Description ="Render the document handle into a PDF and return the bytes (Binary Data). Keep output under ~5 MB or use RenderAndStore.", ReturnName = "Pdf")]
    byte[] Render([OSParameter(Description = "The document handle from Create / the last Add action.")] byte[] document);

    [OSAction(IconResourceName = IconNames.Action, Description ="Single-call power path: render a complete document described as JSON (full element tree, arbitrarily nested) in ONE call — no chained Add/Begin round-trips. See the JSON schema in the docs.", ReturnName = "Pdf")]
    byte[] RenderJson([OSParameter(DataType = OSDataType.Text, Description = "A JSON document spec: { options, root:{type,children:[...]}, header, footer, watermark, title, author, subject }.")] string documentJson);

    [OSAction(IconResourceName = IconNames.Action, Description ="Render the document and POST the PDF bytes to a callback URL (your REST endpoint or a pre-signed S3 URL), returning only a status. Use for large PDFs beyond the 5.5 MB inline limit.", ReturnName = "Result")]
    UploadResult RenderAndStore(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "HTTPS URL the PDF bytes are POSTed to.")] string callbackUrl,
        [OSParameter(DataType = OSDataType.Text, Description = "Optional bearer token (use token auth, not IP filtering).")] string authToken);

    // ---- content primitives -------------------------------------------------

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a heading. Level 1-3 sets a default size (20/16/13 pt) unless the style overrides it.", ReturnName = "UpdatedDocument")]
    byte[] AddHeading(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Heading text.")] string text,
        [OSParameter(DataType = OSDataType.Integer, Description = "Heading level 1-3.")] int level,
        [OSParameter(Description = "Optional style overrides.")] TextStyle style);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a paragraph of text. Use newline characters for line breaks.", ReturnName = "UpdatedDocument")]
    byte[] AddText(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Paragraph text.")] string text,
        [OSParameter(Description = "Optional text style.")] TextStyle style);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a paragraph composed of multiple styled runs (mixed fonts/colors/weights, hyperlinks, super/subscript).", ReturnName = "UpdatedDocument")]
    byte[] AddRichText(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(Description = "The ordered runs that make up the paragraph.")] IEnumerable<TextSpan> spans,
        [OSParameter(DataType = OSDataType.Text, Description = "Paragraph alignment: Left, Center, Right or Justify.")] string alignment);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a bullet or numbered list.", ReturnName = "UpdatedDocument")]
    byte[] AddList(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(Description = "List item texts.")] IEnumerable<string> items,
        [OSParameter(DataType = OSDataType.Boolean, Description = "True = numbered (1. 2. 3.), False = bulleted.")] bool ordered,
        [OSParameter(Description = "Optional item text style.")] TextStyle style);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append an SVG vector graphic (the escape hatch for charts/custom graphics produced elsewhere).", ReturnName = "UpdatedDocument")]
    byte[] AddSvg(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "SVG markup.")] string svg,
        [OSParameter(Description = "Fit, max width and alignment.")] ImageOptions options);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append an image (PNG/JPEG bytes).", ReturnName = "UpdatedDocument")]
    byte[] AddImage(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(Description = "Image bytes (PNG/JPEG).")] byte[] image,
        [OSParameter(Description = "Fit, max width and alignment.")] ImageOptions options);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a table.", ReturnName = "UpdatedDocument")]
    byte[] AddTable(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(Description = "Columns, rows and styling.")] TableSpec table);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a horizontal divider line.", ReturnName = "UpdatedDocument")]
    byte[] AddDivider(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Hex color, e.g. '#cccccc'. Blank = grey.")] string colorHex,
        [OSParameter(DataType = OSDataType.Decimal, Description = "Line thickness in points. Default 1.")] decimal thickness);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append vertical empty space.", ReturnName = "UpdatedDocument")]
    byte[] AddSpace(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Decimal, Description = "Height in points.")] decimal heightPoints);

    [OSAction(IconResourceName = IconNames.Action, Description ="Force the following content onto a new page.", ReturnName = "UpdatedDocument")]
    byte[] AddPageBreak([OSParameter(Description = "The document handle.")] byte[] document);

    // ---- layout containers (nestable) --------------------------------------

    [OSAction(IconResourceName = IconNames.Action, Description ="Begin a horizontal row. Each block added before EndRow becomes a side-by-side column (use BeginCell for explicit widths).", ReturnName = "UpdatedDocument")]
    byte[] BeginRow(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Decimal, Description = "Spacing between columns in points. 0 = default.")] decimal spacing);

    [OSAction(IconResourceName = IconNames.Action, Description ="End the current row.", ReturnName = "UpdatedDocument")]
    byte[] EndRow([OSParameter(Description = "The document handle.")] byte[] document);

    [OSAction(IconResourceName = IconNames.Action, Description ="Begin a vertical group (stacks its content). Useful for grouping or as a row cell.", ReturnName = "UpdatedDocument")]
    byte[] BeginColumn(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Decimal, Description = "Spacing between items in points. 0 = default.")] decimal spacing);

    [OSAction(IconResourceName = IconNames.Action, Description ="End the current column.", ReturnName = "UpdatedDocument")]
    byte[] EndColumn([OSParameter(Description = "The document handle.")] byte[] document);

    [OSAction(IconResourceName = IconNames.Action, Description ="Begin a width-controlled cell inside a row. WidthType 'Relative' (proportional) or 'Constant' (points).", ReturnName = "UpdatedDocument")]
    byte[] BeginCell(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "'Relative' or 'Constant'.")] string widthType,
        [OSParameter(DataType = OSDataType.Decimal, Description = "Relative weight, or width in points for Constant.")] decimal width);

    [OSAction(IconResourceName = IconNames.Action, Description ="End the current cell.", ReturnName = "UpdatedDocument")]
    byte[] EndCell([OSParameter(Description = "The document handle.")] byte[] document);

    [OSAction(IconResourceName = IconNames.Action, Description ="Begin a decorated box (background, border, padding) that wraps its content — for callouts and cards.", ReturnName = "UpdatedDocument")]
    byte[] BeginBox(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(Description = "Background, border and padding.")] BoxStyle style);

    [OSAction(IconResourceName = IconNames.Action, Description ="End the current box.", ReturnName = "UpdatedDocument")]
    byte[] EndBox([OSParameter(Description = "The document handle.")] byte[] document);

    // ---- recurring header / footer -----------------------------------------

    [OSAction(IconResourceName = IconNames.Action, Description ="Set repeating header text shown on every page.", ReturnName = "UpdatedDocument")]
    byte[] SetHeader(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Header text.")] string text,
        [OSParameter(Description = "Optional header text style.")] TextStyle style);

    [OSAction(IconResourceName = IconNames.Action, Description ="Set repeating footer text and optional 'page X / Y' numbering.", ReturnName = "UpdatedDocument")]
    byte[] SetFooter(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Footer text (optional).")] string text,
        [OSParameter(Description = "Optional footer text style.")] TextStyle style,
        [OSParameter(DataType = OSDataType.Boolean, Description = "Show 'page X / Y' numbering.")] bool showPageNumbers);

    [OSAction(IconResourceName = IconNames.Action, Description ="Set a centered watermark drawn behind the content on every page.", ReturnName = "UpdatedDocument")]
    byte[] SetWatermark(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Watermark text, e.g. 'DRAFT'.")] string text,
        [OSParameter(Description = "Optional style (defaults to large faint grey).")] TextStyle style);

    [OSAction(IconResourceName = IconNames.Action, Description ="Set PDF document metadata (shown in the viewer's properties).", ReturnName = "UpdatedDocument")]
    byte[] SetMetadata(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Document title.")] string title,
        [OSParameter(DataType = OSDataType.Text, Description = "Author.")] string author,
        [OSParameter(DataType = OSDataType.Text, Description = "Subject.")] string subject);

    // ---- runtime fonts ------------------------------------------------------

    [OSAction(IconResourceName = IconNames.Action, Description ="Register a custom font at runtime from its file bytes (TTF/OTF) so documents can use brand fonts without rebuilding the library. The font travels in the handle and is applied at render. Then set FontFamily (in options or any TextStyle) to the name you give here. Note: fonts are large — for big/brand fonts prefer RegisterFontFromUrl to keep the handle under the 5.5 MB payload limit.", ReturnName = "UpdatedDocument")]
    byte[] RegisterFont(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Family name to use in FontFamily, e.g. \"Acme Sans\".")] string fontName,
        [OSParameter(Description = "TTF/OTF font file bytes.")] byte[] fontBytes);

    [OSAction(IconResourceName = IconNames.Action, Description ="Register a custom font from a URL — the library downloads it server-side at render (cached per process). Only the small URL travels in the handle, so this scales to large/brand fonts without hitting the 5.5 MB payload limit. Host the TTF/OTF on your app's resource URL, a CDN, or S3, then set FontFamily to this name.", ReturnName = "UpdatedDocument")]
    byte[] RegisterFontFromUrl(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Family name to use in FontFamily, e.g. \"Acme Sans\".")] string fontName,
        [OSParameter(DataType = OSDataType.Text, Description = "HTTPS URL of the TTF/OTF file.")] string fontUrl);

    // ---- navigation: sections + table of contents ---------------------------

    [OSAction(IconResourceName = IconNames.Action, Description ="Begin a named section — a navigation target for a table of contents or an internal section link, and a logical group that may span pages.", ReturnName = "UpdatedDocument")]
    byte[] BeginSection(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(DataType = OSDataType.Text, Description = "Unique section name (referenced by AddTableOfContents and TextSpan.SectionLink).")] string name);

    [OSAction(IconResourceName = IconNames.Action, Description ="End the current section.", ReturnName = "UpdatedDocument")]
    byte[] EndSection([OSParameter(Description = "The document handle.")] byte[] document);

    [OSAction(IconResourceName = IconNames.Action, Description ="Append a clickable table of contents. Each entry jumps to a named section and shows that section's page number automatically.", ReturnName = "UpdatedDocument")]
    byte[] AddTableOfContents(
        [OSParameter(Description = "The document handle.")] byte[] document,
        [OSParameter(Description = "Ordered entries: label, target section name, indent level.")] IEnumerable<TocEntry> entries,
        [OSParameter(Description = "Optional text style for entries.")] TextStyle style);
}
