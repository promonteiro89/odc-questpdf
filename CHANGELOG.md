# Changelog

## 1.0.0 — 2026-06-21

First stable release. Generate PDFs server-side in OutSystems Developer Cloud,
no templates: compose any document from primitives. Proven on the ODC runtime.

### Features
- Builder API (chained handle) plus single-call `RenderJson`
- Rich text (mixed styles, hyperlinks, super/subscript, highlight), headings, bullet/numbered lists, images, SVG
- Tables with per-cell styling and column spans
- Nestable layout: rows, cells, columns, decorated boxes
- Navigation: sections plus a clickable table of contents with automatic page numbers
- Bring-your-own fonts at runtime, by bytes or URL (no rebuild)
- Headers, footers, watermarks, page numbers, PDF metadata
- `RenderAndStore` for PDFs over the 5.5 MB payload limit

### Quality
- Input validation with clear, actionable errors
- 21 automated tests (xUnit + PdfPig) and GitHub Actions CI
- `ILogger` tracing via ODC Custom Code Logging & Tracing
- ~9.9 MB package (linux-x64)
