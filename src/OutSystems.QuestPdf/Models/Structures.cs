using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.QuestPdf.Models;

// OutSystems-facing structures. The document itself is NOT a structure — it is an
// opaque Binary Data "handle" passed in and out of every builder action. These
// structs only describe styling and content for individual primitives.
// byte[] fields omit DataType so the SDK infers Binary Data.

/// <summary>Page setup and default text style. Passed once to Create.</summary>
[OSStructure(Description = "Page setup and default text style for a new document.")]
public struct DocumentOptions
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Page size: A4, A3, A5, Letter or Legal. Defaults to A4.")]
    public string PageSize;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Use landscape orientation. Default is portrait.")]
    public bool Landscape;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Page margin in centimetres. Defaults to 2.")]
    public decimal MarginCm;

    [OSStructureField(DataType = OSDataType.Text, Description = "Default body font family. Defaults to 'Lato'.")]
    public string FontFamily;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Default body font size in points. Defaults to 11.")]
    public decimal FontSize;
}

/// <summary>Reusable text styling. Empty/zero fields fall back to the document default.</summary>
[OSStructure(Description = "Text styling. Leave a field blank/zero to inherit the document default.")]
public struct TextStyle
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Font family. Blank = document default.")]
    public string FontFamily;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Font size in points. 0 = inherit.")]
    public decimal FontSize;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Bold.")]
    public bool Bold;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Italic.")]
    public bool Italic;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Underline.")]
    public bool Underline;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Strikethrough.")]
    public bool Strikethrough;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Render as superscript.")]
    public bool Superscript;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Render as subscript.")]
    public bool Subscript;

    [OSStructureField(DataType = OSDataType.Text, Description = "Hex text color, e.g. '#222222'. Blank = default.")]
    public string ColorHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Hex highlight/background color behind the text. Blank = none.")]
    public string BackgroundColorHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Horizontal alignment: Left, Center, Right or Justify. Blank = Left.")]
    public string Alignment;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Line height multiplier (e.g. 1.4). 0 = default.")]
    public decimal LineHeight;
}

/// <summary>One styled run of text within a rich-text paragraph, optionally a hyperlink.</summary>
[OSStructure(Description = "A styled run of text within a rich-text paragraph; optionally a hyperlink.")]
public struct TextSpan
{
    [OSStructureField(DataType = OSDataType.Text, Description = "The text of this run.")]
    public string Text;

    [OSStructureField(Description = "Style for this run.")]
    public TextStyle Style;

    [OSStructureField(DataType = OSDataType.Text, Description = "Optional URL — makes this run a clickable hyperlink.")]
    public string Hyperlink;

    [OSStructureField(DataType = OSDataType.Text, Description = "Optional section name — makes this run an internal link that jumps to that section (use BeginSection to define it).")]
    public string SectionLink;
}

/// <summary>One entry in a table of contents (clickable, with an auto page number).</summary>
[OSStructure(Description = "A table-of-contents entry: a clickable label that jumps to a named section, with its page number.")]
public struct TocEntry
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Visible label, e.g. the heading text.")]
    public string Label;

    [OSStructureField(DataType = OSDataType.Text, Description = "The section name to jump to (must match a BeginSection).")]
    public string SectionName;

    [OSStructureField(DataType = OSDataType.Integer, Description = "Indent level for sub-entries (0 = top level).")]
    public int Indent;
}

/// <summary>How an image (or SVG) is fitted and aligned.</summary>
[OSStructure(Description = "How an image or SVG is sized and aligned within the document.")]
public struct ImageOptions
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Fit mode: FitWidth (default), FitArea or Original.")]
    public string Fit;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Maximum width in points. 0 = no constraint.")]
    public decimal MaxWidthPoints;

    [OSStructureField(DataType = OSDataType.Text, Description = "Horizontal alignment: Left (default), Center or Right.")]
    public string Alignment;
}

/// <summary>Border / background / padding decoration for a Box container.</summary>
[OSStructure(Description = "Border, background and padding for a Box container (callouts, cards).")]
public struct BoxStyle
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Background hex color. Blank = none.")]
    public string BackgroundHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Border hex color. Blank = grey when thickness > 0.")]
    public string BorderColorHex;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Border thickness in points. 0 = no border.")]
    public decimal BorderThickness;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Inner padding in points.")]
    public decimal Padding;
}

/// <summary>A table primitive: headers, optional column widths, rows and styling.</summary>
[OSStructure(Description = "A table: column headers, optional relative widths, rows and styling.")]
public struct TableSpec
{
    [OSStructureField(Description = "Header cell texts, left to right. Empty = no header row.")]
    public IEnumerable<string> Columns;

    [OSStructureField(Description = "Relative column widths (e.g. 3,1,1). Empty = equal widths.")]
    public IEnumerable<decimal> ColumnWidths;

    [OSStructureField(Description = "Data rows.")]
    public IEnumerable<TableRow> Rows;

    [OSStructureField(Description = "Style for header cells.")]
    public TextStyle HeaderStyle;

    [OSStructureField(Description = "Style for body cells (used when a row has plain Cells).")]
    public TextStyle BodyStyle;

    [OSStructureField(DataType = OSDataType.Text, Description = "Header background hex color. Blank = light grey.")]
    public string HeaderBackgroundHex;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Draw full cell borders. Default = bottom rule only.")]
    public bool ShowBorders;
}

/// <summary>A table row. Use Cells for plain text, or RichCells for per-cell control.</summary>
[OSStructure(Description = "A table row. Use Cells for plain text, or RichCells for per-cell styling and column spans.")]
public struct TableRow
{
    [OSStructureField(Description = "Plain cell values, aligned to the column order. Ignored if RichCells is set.")]
    public IEnumerable<string> Cells;

    [OSStructureField(Description = "Styled cells with optional background and column span. Takes precedence over Cells.")]
    public IEnumerable<TableCell> RichCells;
}

/// <summary>A styled table cell.</summary>
[OSStructure(Description = "A styled table cell with optional background and column span.")]
public struct TableCell
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Cell text.")]
    public string Text;

    [OSStructureField(Description = "Cell text style.")]
    public TextStyle Style;

    [OSStructureField(DataType = OSDataType.Text, Description = "Cell background hex color. Blank = none.")]
    public string BackgroundHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Cell text alignment: Left, Center or Right.")]
    public string Alignment;

    [OSStructureField(DataType = OSDataType.Integer, Description = "Number of columns this cell spans. 1 = no span.")]
    public int ColumnSpan;
}

/// <summary>Result of a store-via-REST render.</summary>
[OSStructure(Description = "Result of a store-via-REST render.")]
public struct UploadResult
{
    [OSStructureField(DataType = OSDataType.Boolean, Description = "True if the PDF was generated and the upload returned a success status.")]
    public bool Success;

    [OSStructureField(DataType = OSDataType.Integer, Description = "HTTP status code returned by the callback URL (0 if the request failed before a response).")]
    public int StatusCode;

    [OSStructureField(DataType = OSDataType.LongInteger, Description = "Generated PDF size in bytes.")]
    public long SizeBytes;

    [OSStructureField(DataType = OSDataType.Text, Description = "Error detail, empty when Success is true.")]
    public string Error;
}
