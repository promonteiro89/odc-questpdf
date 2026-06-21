using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;

namespace OutSystems.QuestPdf.Models;

[OSStructure(Description = "Document-level setup: page size, default text, metadata, header/footer, watermark and fonts.")]
public struct DocumentOptions
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Page size: A4, A3, A5, Letter or Legal. Defaults to A4.")]
    public string PageSize;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Landscape orientation. Default is portrait.")]
    public bool Landscape;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Page margin in centimetres. Defaults to 2.")]
    public decimal MarginCm;

    [OSStructureField(DataType = OSDataType.Text, Description = "Default body font family. Defaults to 'Lato'.")]
    public string FontFamily;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Default body font size in points. Defaults to 11.")]
    public decimal FontSize;

    [OSStructureField(DataType = OSDataType.Text, Description = "PDF metadata title.")]
    public string Title;

    [OSStructureField(DataType = OSDataType.Text, Description = "PDF metadata author.")]
    public string Author;

    [OSStructureField(DataType = OSDataType.Text, Description = "PDF metadata subject.")]
    public string Subject;

    [OSStructureField(Description = "Repeating page header (leave Text empty for none).")]
    public HeaderFooter Header;

    [OSStructureField(Description = "Repeating page footer; set ShowPageNumbers for 'page X / Y'.")]
    public HeaderFooter Footer;

    [OSStructureField(Description = "Diagonal watermark behind content (leave Text empty for none).")]
    public Watermark Watermark;

    [OSStructureField(Description = "Fonts to make available at render time (by URL or bytes). Reference by Name in FontFamily.")]
    public IEnumerable<FontRef> Fonts;
}

[OSStructure(Description = "Repeating header/footer text.")]
public struct HeaderFooter
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Text to repeat on every page. Empty = none.")]
    public string Text;

    [OSStructureField(Description = "Text style.")]
    public TextStyle Style;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Footer only: append 'page X / Y' numbering.")]
    public bool ShowPageNumbers;
}

[OSStructure(Description = "A diagonal watermark drawn behind the content on every page.")]
public struct Watermark
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Watermark text, e.g. 'DRAFT'. Empty = none.")]
    public string Text;

    [OSStructureField(Description = "Optional style (defaults to large faint grey).")]
    public TextStyle Style;
}

[OSStructure(Description = "A custom font available at render time. Provide Url (recommended) or Bytes; reference it by Name in FontFamily.")]
public struct FontRef
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Family name to use in FontFamily, e.g. 'Acme Sans'.")]
    public string Name;

    [OSStructureField(DataType = OSDataType.Text, Description = "HTTPS URL of the TTF/OTF file (downloaded server-side, cached). Preferred — keeps the payload small.")]
    public string Url;

    [OSStructureField(Description = "TTF/OTF bytes (alternative to Url; large fonts inflate the payload).")]
    public byte[] Bytes;
}

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

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Superscript.")]
    public bool Superscript;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Subscript.")]
    public bool Subscript;

    [OSStructureField(DataType = OSDataType.Text, Description = "Hex text color, e.g. '#222222'. Blank = default.")]
    public string ColorHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Hex highlight color behind the text. Blank = none.")]
    public string BackgroundColorHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Alignment: Left, Center, Right or Justify. Blank = Left.")]
    public string Alignment;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Line height multiplier (e.g. 1.4). 0 = default.")]
    public decimal LineHeight;
}

[OSStructure(Description = "A styled run within a RichText block; optionally a hyperlink or an internal section jump.")]
public struct TextSpan
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Run text.")]
    public string Text;

    [OSStructureField(Description = "Run style.")]
    public TextStyle Style;

    [OSStructureField(DataType = OSDataType.Text, Description = "Optional URL — makes this run a clickable hyperlink.")]
    public string Hyperlink;

    [OSStructureField(DataType = OSDataType.Text, Description = "Optional section name — makes this run jump to a BeginSection block.")]
    public string SectionLink;
}

[OSStructure(Description = "A table: column headers, optional relative widths, rows and styling.")]
public struct TableSpec
{
    [OSStructureField(Description = "Header cell texts. Empty = no header row.")]
    public IEnumerable<string> Columns;

    [OSStructureField(Description = "Relative column widths (e.g. 3,1,1). Empty = equal.")]
    public IEnumerable<decimal> ColumnWidths;

    [OSStructureField(Description = "Data rows.")]
    public IEnumerable<TableRow> Rows;

    [OSStructureField(Description = "Header cell style.")]
    public TextStyle HeaderStyle;

    [OSStructureField(Description = "Body cell style (for plain Cells).")]
    public TextStyle BodyStyle;

    [OSStructureField(DataType = OSDataType.Text, Description = "Header background hex color. Blank = light grey.")]
    public string HeaderBackgroundHex;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "Draw full cell borders. Default = bottom rule only.")]
    public bool ShowBorders;
}

[OSStructure(Description = "A table row. Use Cells for plain text, or RichCells for per-cell styling and column spans.")]
public struct TableRow
{
    [OSStructureField(Description = "Plain cell values. Ignored if RichCells is set.")]
    public IEnumerable<string> Cells;

    [OSStructureField(Description = "Styled cells (background, alignment, column span). Takes precedence over Cells.")]
    public IEnumerable<TableCell> RichCells;
}

[OSStructure(Description = "A styled table cell with optional background and column span.")]
public struct TableCell
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Cell text.")]
    public string Text;

    [OSStructureField(Description = "Cell text style.")]
    public TextStyle Style;

    [OSStructureField(DataType = OSDataType.Text, Description = "Cell background hex color. Blank = none.")]
    public string BackgroundHex;

    [OSStructureField(DataType = OSDataType.Text, Description = "Cell alignment: Left, Center or Right.")]
    public string Alignment;

    [OSStructureField(DataType = OSDataType.Integer, Description = "Columns this cell spans. 1 = no span.")]
    public int ColumnSpan;
}

[OSStructure(Description = "Border, background and padding for a BeginBox block (callouts, cards).")]
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

[OSStructure(Description = "A table-of-contents entry: a clickable label that jumps to a section, with its page number.")]
public struct TocEntry
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Visible label.")]
    public string Label;

    [OSStructureField(DataType = OSDataType.Text, Description = "Target section name (must match a BeginSection block).")]
    public string SectionName;

    [OSStructureField(DataType = OSDataType.Integer, Description = "Indent level for sub-entries (0 = top level).")]
    public int Indent;
}

[OSStructure(Description = "One content block. Set Type plus the fields for that type. Types: Heading, Text, RichText, List, Image, Svg, Table, Divider, Space, PageBreak, BeginRow/EndRow, BeginColumn/EndColumn, BeginCell/EndCell, BeginBox/EndBox, BeginSection/EndSection, Toc.")]
public struct Block
{
    [OSStructureField(DataType = OSDataType.Text, Description = "Block type (see the list above). Required.", IsMandatory = true)]
    public string Type;

    [OSStructureField(DataType = OSDataType.Text, Description = "Text — for Heading, Text.")]
    public string Text;

    [OSStructureField(DataType = OSDataType.Integer, Description = "Heading level 1-3.")]
    public int Level;

    [OSStructureField(Description = "Text style — for Heading, Text.")]
    public TextStyle Style;

    [OSStructureField(Description = "Runs — for RichText.")]
    public IEnumerable<TextSpan> Spans;

    [OSStructureField(Description = "Items — for List.")]
    public IEnumerable<string> Items;

    [OSStructureField(DataType = OSDataType.Boolean, Description = "List: true = numbered, false = bulleted.")]
    public bool Ordered;

    [OSStructureField(Description = "Image bytes (PNG/JPEG) — for Image.")]
    public byte[] Image;

    [OSStructureField(DataType = OSDataType.Text, Description = "SVG markup — for Svg.")]
    public string Svg;

    [OSStructureField(DataType = OSDataType.Text, Description = "Fit — for Image/Svg: FitWidth (default), FitArea or Original.")]
    public string Fit;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Max width in points — for Image/Svg. 0 = none.")]
    public decimal MaxWidthPoints;

    [OSStructureField(DataType = OSDataType.Text, Description = "Alignment — for Image/Svg: Left (default), Center or Right.")]
    public string Alignment;

    [OSStructureField(Description = "Table — for Table.")]
    public TableSpec Table;

    [OSStructureField(Description = "Box style — for BeginBox.")]
    public BoxStyle Box;

    [OSStructureField(DataType = OSDataType.Text, Description = "Cell width type — for BeginCell: 'Relative' or 'Constant'.")]
    public string WidthType;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Cell width — for BeginCell (weight, or points for Constant).")]
    public decimal Width;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Spacing in points — for BeginRow/BeginColumn.")]
    public decimal Spacing;

    [OSStructureField(DataType = OSDataType.Text, Description = "Hex color — for Divider. Blank = grey.")]
    public string ColorHex;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Thickness in points — for Divider. Default 1.")]
    public decimal Thickness;

    [OSStructureField(DataType = OSDataType.Decimal, Description = "Height in points — for Space.")]
    public decimal Height;

    [OSStructureField(DataType = OSDataType.Text, Description = "Section name — for BeginSection (unique; targeted by Toc and SectionLink).")]
    public string SectionName;

    [OSStructureField(Description = "Entries — for Toc.")]
    public IEnumerable<TocEntry> Toc;
}

[OSStructure(Description = "Result of a store-via-REST render.")]
public struct UploadResult
{
    [OSStructureField(DataType = OSDataType.Boolean, Description = "True if the PDF was generated and the upload returned a success status.")]
    public bool Success;

    [OSStructureField(DataType = OSDataType.Integer, Description = "HTTP status code from the callback (0 if the request failed before a response).")]
    public int StatusCode;

    [OSStructureField(DataType = OSDataType.LongInteger, Description = "Generated PDF size in bytes.")]
    public long SizeBytes;

    [OSStructureField(DataType = OSDataType.Text, Description = "Error detail, empty when Success is true.")]
    public string Error;
}
