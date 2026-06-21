# Block reference

`Render(options, content)` takes `DocumentOptions` and an ordered `List<Block>`.
Build the list with `ListAppend`. Each `Block` has a `Type` plus the fields below;
unused fields are ignored. Nesting uses `Begin…`/`End…` blocks (must be balanced).

## DocumentOptions
| Field | Type | Notes |
|---|---|---|
| `PageSize` | Text | A4 (default), A3, A5, Letter, Legal |
| `Landscape` | Boolean | default portrait |
| `MarginCm` | Decimal | default 2 |
| `FontFamily` | Text | default `Lato` |
| `FontSize` | Decimal | default 11 |
| `Title` / `Author` / `Subject` | Text | PDF metadata |
| `Header` / `Footer` | HeaderFooter | `Text`, `Style`, (footer) `ShowPageNumbers` |
| `Watermark` | Watermark | `Text`, `Style` |
| `Fonts` | List&lt;FontRef&gt; | `{ Name, Url }` or `{ Name, Bytes }` |

## Block fields by type
| Type | Fields |
|---|---|
| `Heading` | `Text`, `Level` (1-3), `Style` |
| `Text` | `Text`, `Style` (newlines honored) |
| `RichText` | `Spans` (List&lt;TextSpan&gt;), `Alignment` |
| `List` | `Items` (List&lt;Text&gt;), `Ordered`, `Style` |
| `Image` | `Image` (Binary), `Fit`, `MaxWidthPoints`, `Alignment` |
| `Svg` | `Svg` (Text), `Fit`, `MaxWidthPoints`, `Alignment` |
| `Table` | `Table` (TableSpec) |
| `Divider` | `ColorHex`, `Thickness` |
| `Space` | `Height` |
| `PageBreak` | — |
| `BeginRow` / `EndRow` | `Spacing` |
| `BeginColumn` / `EndColumn` | `Spacing` |
| `BeginCell` / `EndCell` | `WidthType` (`Relative`/`Constant`), `Width` |
| `BeginBox` / `EndBox` | `Box` (BoxStyle) |
| `BeginSection` / `EndSection` | `SectionName` (unique) |
| `Toc` | `Toc` (List&lt;TocEntry&gt;), `Style` |

`Fit`: `FitWidth` (default), `FitArea`, `Original`. `Alignment`: `Left`, `Center`,
`Right` (text also `Justify`).

## Shared structs
- **TextStyle**: `FontFamily`, `FontSize`, `Bold`, `Italic`, `Underline`,
  `Strikethrough`, `Superscript`, `Subscript`, `ColorHex`, `BackgroundColorHex`,
  `Alignment`, `LineHeight`. Blank/zero = inherit.
- **TextSpan** (RichText run): `Text`, `Style`, `Hyperlink` (URL), `SectionLink`
  (section name to jump to).
- **TableSpec**: `Columns` (List&lt;Text&gt;), `ColumnWidths` (List&lt;Decimal&gt;),
  `Rows` (List&lt;TableRow&gt;), `HeaderStyle`, `BodyStyle`, `HeaderBackgroundHex`,
  `ShowBorders`.
- **TableRow**: `Cells` (List&lt;Text&gt;) or `RichCells` (List&lt;TableCell&gt;).
- **TableCell**: `Text`, `Style`, `BackgroundHex`, `Alignment`, `ColumnSpan`.
- **BoxStyle**: `BackgroundHex`, `BorderColorHex`, `BorderThickness`, `Padding`.
- **TocEntry**: `Label`, `SectionName`, `Indent`.
- **FontRef**: `Name` + (`Url` or `Bytes`).

## Validation
Errors are raised at `Render` and name the offending block, e.g.
`Block #5 ('Image'): Image block 'Image' requires non-empty image bytes.` Checks:
valid hex colors, recognizable image bytes, known block types, balanced `Begin/End`,
unique section names, and valid font URLs.

## Colors
Hex `#RRGGBB` or `#AARRGGBB`. Invalid values fail with a clear error.
