# QuestPDF for ODC

[![Platform](https://img.shields.io/badge/Platform-OutSystems_ODC-red.svg)](https://www.outsystems.com/odc/)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![QuestPDF](https://img.shields.io/badge/PDF-QuestPDF-orange.svg)](https://www.questpdf.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Server-side PDF generation for **OutSystems Developer Cloud (ODC)**, powered by
[QuestPDF](https://github.com/QuestPDF/QuestPDF). No templates — you compose any
document from primitives and render it in **one call**.

You build a **List of `Block`** records in OutSystems (with `ListAppend`) and pass it
to `Render` once. That's a single remote round-trip — no per-element calls, no growing
handle. Nesting (rows, cells, boxes, sections) is expressed with `Begin…`/`End…` blocks.

## Quick start (in OutSystems)

```
Content = new List<Block>
ListAppend(Content, { Type: "Heading", Text: "Invoice INV-2042", Level: 1 })
ListAppend(Content, { Type: "BeginRow", Spacing: 20 })
ListAppend(Content,   { Type: "Text", Text: "Acme Corp" })
ListAppend(Content,   { Type: "Text", Text: "Bill to: Globex SA", Style: { Alignment: "Right" } })
ListAppend(Content, { Type: "EndRow" })
ListAppend(Content, { Type: "Table", Table: LineItems })
ListAppend(Content, { Type: "Text", Text: "Total: 44.48", Style: { Bold: true, Alignment: "Right" } })

Pdf = Render(Options, Content).Pdf          // one server-action call -> Binary Data
```

The `ListAppend`s are local and cheap; only `Render` is a remote call.

## Actions

| Action | Returns | Purpose |
|---|---|---|
| `Render(options, content)` | Binary Data (PDF) | Render the document in one call. |
| `RenderAndStore(options, content, callbackUrl, authToken)` | `UploadResult` | Render and POST the PDF to a URL — for PDFs over the 5.5 MB inline payload limit. |

## Blocks

Each item in `content` is a `Block` with a `Type` plus the fields that apply to it:

| `Type` | Key fields | Renders |
|---|---|---|
| `Heading` | `Text`, `Level` (1-3), `Style` | A heading |
| `Text` | `Text`, `Style` | A paragraph (newlines honored) |
| `RichText` | `Spans`, `Alignment` | Mixed runs — bold/color, hyperlinks, super/subscript, section links |
| `List` | `Items`, `Ordered`, `Style` | Bullet or numbered list |
| `Image` | `Image`, `Fit`, `MaxWidthPoints`, `Alignment` | A PNG/JPEG image |
| `Svg` | `Svg`, `Fit`, `MaxWidthPoints`, `Alignment` | A vector graphic |
| `Table` | `Table` | A table — plain `Cells` or `RichCells` (per-cell style, background, column span) |
| `Divider` | `ColorHex`, `Thickness` | A horizontal rule |
| `Space` | `Height` | Vertical space |
| `PageBreak` | — | A page break |
| `BeginRow`/`EndRow` | `Spacing` | Side-by-side columns |
| `BeginCell`/`EndCell` | `WidthType`, `Width` | A width-controlled cell in a row |
| `BeginColumn`/`EndColumn` | `Spacing` | A vertical group |
| `BeginBox`/`EndBox` | `Box` | A decorated box (background, border, padding) |
| `BeginSection`/`EndSection` | `SectionName` | A navigation target for `Toc` / `SectionLink` |
| `Toc` | `Toc`, `Style` | A clickable table of contents with automatic page numbers |

Full field reference: [docs/BLOCKS.md](docs/BLOCKS.md). Styling is via the `TextStyle`
struct (font, size, bold/italic/underline/strikethrough, super/subscript, hex text +
highlight color, alignment, line height) wherever text appears.

## DocumentOptions

Set once and passed to `Render`: `PageSize` (A4/A3/A5/Letter/Legal), `Landscape`,
`MarginCm`, `FontFamily`, `FontSize`, metadata (`Title`/`Author`/`Subject`), `Header`
and `Footer` (text + style, footer `ShowPageNumbers`), `Watermark`, and `Fonts`.

## Fonts — bring your own, at runtime

The component bundles only QuestPDF's **Lato** (the fallback) and ships no other fonts,
so you're not limited to what's baked in. Add fonts via `DocumentOptions.Fonts`
(a list of `FontRef`):

- **By URL** (recommended): `{ Name: "Acme Sans", Url: "https://cdn…/acme.ttf" }` — the
  library downloads it server-side at render and caches it per process. Only the URL
  travels in the payload, so it scales to large/brand fonts.
- **By bytes**: `{ Name: "Acme Sans", Bytes: <ttf> }` — embeds the font in the request
  (heavier; counts against the 5.5 MB payload).

Then set `FontFamily` (in options or any `TextStyle`) to that name. A missing font
silently falls back to Lato, so verify the glyphs you need.

## Project layout

```
src/OutSystems.QuestPdf/
  IQuestPdfGenerator.cs       the [OSInterface] (Render, RenderAndStore)
  QuestPdfGenerator.cs        implementation + license/font bootstrap
  Models/Structures.cs        [OSStructure] DTOs (DocumentOptions, Block, TextStyle, …)
  Internal/BlockCompiler.cs   compiles the Block list into the render tree
  Internal/DocumentSpec.cs    the render tree + cursor mechanics
  Rendering/PdfComposer.cs    walks the tree, renders with QuestPDF
  Internal/                   page setup, fonts, validation, REST delivery
  Resources/Fonts|Icons/      drop .ttf/.png here (auto-embedded)
build/generate_upload_package.* publish + zip for the ODC Portal
tests/OutSystems.QuestPdf.Tests/  xUnit test suite
docs/BLOCKS.md                full block / field reference
```

## Build & test

Requires the **.NET 8 SDK**.

```bash
dotnet build src/OutSystems.QuestPdf
dotnet test tests/OutSystems.QuestPdf.Tests   # 17 tests
```

## Package for ODC

```bash
./build/generate_upload_package.sh       # -> ExternalLibrary_net8.0.zip
# Windows:  .\build\generate_upload_package.ps1
```

Publishes `linux-x64` (framework-dependent) and zips the output (verified to include
QuestPDF's native engine `libQuestPdfSkia.so` + `libqpdf.so` and the bundled `LatoFont`).
Upload it in **ODC Portal → External Logic**.

## Reliability

- **Validation with clear, block-tagged errors.** Bad hex colors, non-image bytes,
  unknown block types, unbalanced `Begin/End`, duplicate section names and bad font
  URLs all fail with a message naming the offending block (e.g. `Block #5 ('Image'): …`).
- **Tested.** A 17-test xUnit suite renders real PDFs and asserts page counts, text,
  metadata and navigation with a managed PDF reader, covers every validation case, and
  verifies `RenderAndStore` and URL fonts end-to-end against a live HTTP listener.
- **Build-time SDK checks.** `CustomCode.Analyzer` validates ODC SDK usage at build.
- **Observability.** The ODC runtime injects an `ILogger` (Custom Code Logging &
  Tracing); the library logs render operation, output size, elapsed time and errors.
  Null-safe, so it also runs locally and in tests.

## License

The wrapper code in this repository is MIT (`LICENSE`); bundled and third-party
components carry their own licenses (`NOTICE.md`). In particular, **QuestPDF** is
dual-licensed — its Community tier is free only **under USD 1M** annual revenue; above
that, set `Professional`/`Enterprise` in `QuestPdfGenerator.LicenseTier` and procure a
license (honour-system enum — legal, not technical). Currently set to **Community**.
