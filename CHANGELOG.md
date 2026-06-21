# Changelog

## 1.0.0 — 2026-06-21

First release. Server-side PDF generation for OutSystems Developer Cloud — no
templates: compose any document from primitives and render it in one call.

### Features
- One-call API: `Render(options, content)`, where `content` is a `List` of `Block`
  records built with `ListAppend`; `RenderAndStore` for PDFs over the 5.5 MB limit.
- Blocks: Heading, Text, RichText (mixed styles, hyperlinks, super/subscript, section
  links), List, Image, Svg, Table (per-cell styling + column spans), Divider, Space,
  PageBreak — with nesting via BeginRow/Cell/Column/Box/Section.
- Navigation: sections plus a clickable table of contents with automatic page numbers.
- Fonts at runtime via `DocumentOptions.Fonts` — by URL (downloaded server-side) or bytes.
- Headers, footers, page numbers, watermark and PDF metadata.

### Quality
- Validation with clear, block-tagged errors.
- 17 automated tests (xUnit + PdfPig).
- `ILogger` tracing via ODC Custom Code Logging & Tracing.
- ~9.9 MB package (linux-x64).
