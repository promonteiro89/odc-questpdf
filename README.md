# OutSystems.QuestPdf — QuestPDF as an ODC External Library

[![CI](https://github.com/promonteiro89/odc-questpdf/actions/workflows/ci.yml/badge.svg)](https://github.com/promonteiro89/odc-questpdf/actions/workflows/ci.yml)

A **builder-style** [QuestPDF](https://github.com/QuestPDF/QuestPDF) wrapper for
**OutSystems Developer Cloud (ODC)**. No templates — you compose a document from
primitives: a document **handle** (Binary Data) flows in and out of every action,
and `Render` turns it into a PDF.

> **Proven on ODC.** Uploaded, called, and rendered inside an ODC tenant — the
> native engine (`libQuestPdfSkia.so`) loads on the ODC Linux runtime. See
> [docs/FEASIBILITY.md](docs/FEASIBILITY.md).

```
doc = Create(options)
doc = SetHeader(doc, "ACME", style)
doc = AddHeading(doc, "Invoice INV-1042", 1, style)
doc = BeginRow(doc, 20)
doc =   BeginCell(doc, "Relative", 1)
doc =     AddText(doc, "Acme Corp" + NewLine() + "Lisbon", style)
doc =   EndCell(doc)
doc =   BeginCell(doc, "Relative", 1)
doc =     AddText(doc, "Bill to: Globex SA", rightAlignedStyle)
doc =   EndCell(doc)
doc = EndRow(doc)
doc = AddTable(doc, table)
doc = AddDivider(doc, "#cccccc", 1)
pdf = Render(doc)
```

## Actions

**Lifecycle**

| Action | Returns | Notes |
|---|---|---|
| `Create(options)` | Document handle | Start a new document (page size, margins, default font). |
| `Render(document)` | Binary Data (PDF) | Flatten the handle to a PDF. |
| `RenderJson(documentJson)` | Binary Data (PDF) | **Single-call power path** — render a whole document tree from JSON in one call (see below). |
| `RenderAndStore(document, callbackUrl, authToken)` | `UploadResult` | POST the PDF to a URL for large docs (over the 5.5 MB limit). |
| `SetMetadata(document, title, author, subject)` | Document handle | PDF document properties shown in the viewer. |
| `RegisterFont(document, fontName, fontBytes)` | Document handle | Register a brand font at runtime from its TTF/OTF bytes — no rebuild. Then set `FontFamily` to that name. |

**Content primitives** — each takes the handle and returns the updated handle

| Action | Adds |
|---|---|
| `AddHeading(doc, text, level, style)` | A heading (level 1-3). |
| `AddText(doc, text, style)` | A paragraph (newlines = line breaks). |
| `AddRichText(doc, spans, alignment)` | A paragraph of mixed styled runs — hyperlinks, super/subscript, per-run color/weight. |
| `AddList(doc, items, ordered, style)` | A bullet or numbered list. |
| `AddImage(doc, image, options)` | A PNG/JPEG image. |
| `AddSvg(doc, svg, options)` | An SVG vector graphic (charts/custom graphics produced elsewhere). |
| `AddTable(doc, table)` | A table — plain `Cells` or `RichCells` with per-cell style, background and column span. |
| `AddTableOfContents(doc, entries, style)` | A clickable TOC — each entry jumps to a named section and shows its page number automatically. |
| `AddDivider(doc, colorHex, thickness)` | A horizontal rule. |
| `AddSpace(doc, heightPoints)` | Vertical empty space. |
| `AddPageBreak(doc)` | A page break. |

**Layout containers** (nestable)

| Action | Purpose |
|---|---|
| `BeginRow(doc, spacing)` / `EndRow(doc)` | Side-by-side columns. |
| `BeginCell(doc, widthType, width)` / `EndCell(doc)` | A width-controlled cell in a row (`"Relative"` or `"Constant"`). |
| `BeginColumn(doc, spacing)` / `EndColumn(doc)` | A vertical group. |
| `BeginBox(doc, style)` / `EndBox(doc)` | A decorated box (background, border, padding) — callouts and cards. |
| `BeginSection(doc, name)` / `EndSection(doc)` | A named navigation target for `AddTableOfContents` and `TextSpan.SectionLink`. |

**Header / footer / watermark**

| Action | Purpose |
|---|---|
| `SetHeader(doc, text, style)` | Repeating header on every page. |
| `SetFooter(doc, text, style, showPageNumbers)` | Repeating footer + optional `page X / Y`. |
| `SetWatermark(doc, text, style)` | Diagonal watermark behind the content on every page. |

Styling is via the `TextStyle` struct (font, size, bold/italic/underline/strikethrough,
super/subscript, hex text + highlight color, alignment, line height) — passed wherever
text appears. A rich-text run can also be an internal jump via `TextSpan.SectionLink`.

**Navigation** uses clickable section links + automatic page numbers (a visible TOC
page and inline cross-references). Note: QuestPDF does not emit a viewer
*outline/bookmark sidebar* — navigation is via the in-document links.

**Branding** — the library and action icons live in `Resources/Icons/app.png` and
`action.png` (512×512, embedded and wired via `IconResourceName`). Replace those two
files to rebrand; no code change needed.

## How the handle works

The `document` passed in and out is an **opaque Binary Data handle** — a serialized
document spec (JSON), *not* a PDF. Each action deserializes it, appends/opens/closes
a node, and re-serializes. `Render` is the only step that runs QuestPDF. Because
the spec lives on the C# side, it supports **arbitrarily nested** layouts
(`BeginRow → BeginCell → BeginRow …`) even though OutSystems structures can't be
recursive.

> **Payload note:** every chained action re-sends the growing handle, and each ODC
> call has a 5.5 MB input+output limit. Add large images late, prefer `RenderAndStore`
> for image-heavy documents — or use `RenderJson` (below) to skip the round-trips.

## Two ways to author — same engine, maximum freedom

The chained builder is ergonomic but is N server calls. For full control in **one
call**, `RenderJson` accepts the entire document tree as JSON — arbitrarily nested,
every primitive — and renders it in a single round-trip. The handle the builder
produces *is* this JSON, so the two are interchangeable.

```json
{
  "options": { "pageSize": "A4", "marginCm": 2, "fontFamily": "Lato", "fontSize": 10 },
  "title": "Invoice INV-1042",
  "watermark": { "text": "PAID" },
  "footer": { "showPageNumbers": true },
  "root": { "type": "Column", "number": 8, "children": [
    { "type": "Heading", "text": "Invoice", "level": 1, "style": { "colorHex": "#1f3b57" } },
    { "type": "Row", "number": 20, "children": [
      { "type": "Cell", "widthType": "Relative", "width": 1, "children": [ { "type": "Text", "text": "Acme Corp" } ] },
      { "type": "Cell", "widthType": "Relative", "width": 1, "children": [ { "type": "Text", "text": "Bill to: Globex", "style": { "alignment": "Right" } } ] }
    ] },
    { "type": "Table", "table": { "columns": ["Item","Qty","Total"], "showBorders": true,
      "rows": [ { "cells": ["Widget","2","19.98"] } ] } }
  ] }
}
```

This is as close to "QuestPDF in C#" as a data interface can get: any combination of
primitives, any nesting depth. The only thing it can't express is render-time C#
*logic* — for arbitrary vector output, use `AddSvg`. Full node reference:
[docs/JSON_SCHEMA.md](docs/JSON_SCHEMA.md).

## Project layout

```
src/OutSystems.QuestPdf/
  IQuestPdfGenerator.cs       the [OSInterface] builder contract
  QuestPdfGenerator.cs        implementation + license/font bootstrap
  Models/Structures.cs        [OSStructure] DTOs (DocumentOptions, TextStyle, TableSpec, …)
  Internal/DocumentSpec.cs    the serializable document tree + cursor mechanics
  Rendering/PdfComposer.cs    walks the tree, renders with QuestPDF
  Internal/                   page setup, font registration, REST delivery
  Resources/Fonts|Icons/      drop .ttf/.png here (auto-embedded)
samples/Smoke/                local end-to-end builder test (writes out.pdf)
build/generate_upload_package.* publish + zip for the ODC Portal
docs/FEASIBILITY.md           verdict, constraints, sources
```

## Build & run locally

Requires the **.NET 8 SDK**.

```bash
dotnet run --project samples/Smoke      # builds out.pdf via the builder API
dotnet test tests/OutSystems.QuestPdf.Tests   # 21 tests: render, pagination, JSON path, navigation, fonts (bytes + URL), validation, metadata, RenderAndStore over HTTP
```

## Fonts — bring your own, at runtime

The component bundles only QuestPDF's **Lato** (always available as a fallback) and
ships no other fonts on purpose — consumers aren't limited to what's baked in, and a
reusable library doesn't carry every brand font. Add any font at runtime:

| Approach | Best for | Cost |
|---|---|---|
| **Default (Lato)** | basic Latin text | none |
| `RegisterFont(doc, name, bytes)` | a small font, or one-shot `RenderJson` | the font rides in the handle (base64) and counts against the 5.5 MB payload — when chaining, register it **last**, right before `Render` |
| `RegisterFontFromUrl(doc, name, url)` | **brand/large fonts, chained building** | only the URL is in the handle; the library downloads the font server-side at render and caches it per process — **recommended for a reusable component** |

Where the app gets the bytes/URL: an OutSystems **app resource** (and its URL), a
**database** Binary column, a **CDN/S3** object, or a user upload. After registering,
set `FontFamily` (in `DocumentOptions` or any `TextStyle`) to the name you chose. A
missing/mismatched font silently falls back to Lato — so verify the glyphs you need.

## Reliability

- **Validation with clear errors.** Bad hex colors and non-image bytes fail at the
  offending `Add` call with a readable message; duplicate section names are rejected;
  an unclosed `BeginRow`/`Cell`/`Box`/`Section` fails at `Render` naming the open
  container; an empty/invalid handle is rejected explicitly; and the library warns
  (via the logger) when the handle approaches the 5.5 MB payload limit.
- **Tested + CI.** A 21-test xUnit suite renders real PDFs and asserts page counts,
  text, metadata and navigation with a managed PDF reader, exercises the JSON path and
  every validation case, and verifies `RenderAndStore` end-to-end against a live HTTP
  listener. A GitHub Actions workflow (`.github/workflows/ci.yml`) runs it, packages
  the ODC zip, and uploads it as an artifact on every push.
- **Build-time SDK checks.** The `CustomCode.Analyzer` validates ODC SDK usage at
  build time.
- **Observability.** The ODC runtime injects an `ILogger` into the constructor
  (Custom Code Logging & Tracing); the library logs render operation, output size,
  elapsed time, and errors. Null-safe, so it also runs locally and in tests.

## License

The wrapper code is MIT (`LICENSE`); third-party components carry their own licenses
(`NOTICE.md`). Note QuestPDF's Community license is free only below USD 1M revenue.

## Package for ODC

```bash
./build/generate_upload_package.sh       # -> ExternalLibrary_net8.0.zip
# Windows:  .\build\generate_upload_package.ps1
```

Publishes `linux-x64` (framework-dependent) and zips the output. Verified locally:
the package is **~9.7 MB** and contains `libQuestPdfSkia.so` + `libqpdf.so` plus the
bundled `LatoFont`. Upload it in **ODC Portal → External Logic**.

## License

QuestPDF Community (free) is valid only **under USD 1M** annual revenue; above that,
set `Professional`/`Enterprise` in `QuestPdfGenerator.LicenseTier` and procure a
license (honour-system enum — legal, not technical). Currently set to **Community**.

## ODC constraints

| Constraint | Limit | Handled by |
|---|---|---|
| Inline payload (in + out) | 5.5 MB | `RenderAndStore`; add big images late |
| Execution timeout | 95 s (+ cold start) | bounded docs; raise app Server Request Timeout |
| Upload ZIP size | ~40 MB | no SkiaSharp packages; embed only needed fonts |
| Filesystem | read-only except `/tmp` | PDF generated fully in memory |
| Fonts | none installed | `UseEnvironmentFonts=false` + embed/register; Lato fallback |

See [docs/FEASIBILITY.md](docs/FEASIBILITY.md) for the full sourced analysis.
