# Benchmark — QuestPDF (this component) vs UltimatePDF (Chromium HTML→PDF)

A head-to-head on a **deliberately complex, multi-page report** generated from one
identical dataset, two ways:

1. **This component** — QuestPDF, code-first, native Skia, in-process.
2. **UltimatePDF's engine** — headless **Chromium** rendering equivalent HTML→PDF,
   driven via Puppeteer with header/footer templates and `printBackground` (the
   same mechanism [UltimatePDF](https://github.com/OutSystems/UltimatePDF-ExternalLogic) uses).

## The document (identical content for both)

600 deterministic transactions rendered into: a cover with an SVG logo, a justified
**rich-text** paragraph (bold, red italic, a hyperlink), four **KPI cards**, an **SVG
bar chart**, a bullet list, a **callout box**, a **600-row multi-page table** with a
repeating header, plus a repeating page header/footer with page numbers and a
diagonal **CONFIDENTIAL watermark**.

## Results

| Metric | QuestPDF (this component) | Chromium HTML→PDF (UltimatePDF engine) | Winner |
|---|---:|---:|:--:|
| **Warm render** (engine only) | **92 ms** | 146 ms | QuestPDF (1.6×) |
| **Cold / per-call** (local) | **211 ms** | 1,881 ms | QuestPDF (8.9×) |
| **Cold start in ODC** (documented) | sub-second after warm Lambda | **> 10 s** browser init per UltimatePDF docs | QuestPDF |
| **Output PDF size** | **173 KB** | 1.62 MB | QuestPDF (9.4×) |
| Per-row output | 296 B | 2,765 B | QuestPDF |
| Rows before output hits 5 MB | **~16,900** | ~1,800 | QuestPDF |
| Pages produced | 18 | 15 | — (Chromium denser) |
| Input/handle payload | 88 KB (builder spec) | 92 KB (HTML) | ~tie |
| **Component package size** | **9.7 MB** zip | bundles Chromium (local Chrome 683 MB for reference) | QuestPDF (~70×) |
| Visual fidelity | High | High | tie |

## What this means on ODC

- **Cold start is the decisive difference.** UltimatePDF runs as external logic that
  launches Chromium **per invocation** — so its *cold* number is the realistic
  per-call cost, and ODC docs cite **> 10 s** first calls. QuestPDF runs in-process,
  so after the first call in a warm Lambda the **warm** number is realistic. The
  practical per-document latency gap is therefore far larger than warm-vs-warm.
- **Payload ceiling (5.5 MB in+out).** Chromium output is ~9× larger: this report
  would cross 5 MB at **~1,800 rows**, forcing the store-via-REST pattern. QuestPDF
  reaches 5 MB only near **~16,900 rows** — most documents return inline.
- **Package size (40 MB zip cap).** QuestPDF is a self-contained **9.7 MB**. Bundling
  Chromium is the dominant cost and engineering constraint of the HTML→PDF approach.
- **Fidelity is a tie** for this report — both produced clean, professional output.

## When to use which

**This QuestPDF component** — high-volume, data-driven, server-generated documents
(invoices, statements, reports, certificates). Faster, ~9× smaller output, ~70×
smaller package, no browser cold start, deterministic layout, comfortably under the
ODC payload limit for large tabular data. Trade-off: composed in code/builder, not
HTML/CSS; arbitrary CSS/JS layout isn't available (mitigated by `AddSvg`).

**UltimatePDF (Chromium)** — when the document already exists as a **web page**, needs
**full CSS/HTML fidelity**, or reuses existing web templates / JS chart libraries, and
volume is modest. Trade-off: heavy package, slow per-call cold start, larger output
(payload pressure).

## Methodology & honest caveats

- **Same data, same content.** A single benchmark program ([samples/Benchmark](../samples/Benchmark/Program.cs))
  emits both the QuestPDF PDF and the equivalent HTML from the identical 600-row dataset.
- **QuestPDF:** this component, Release build, in-process. Warm = mean of 12 renders;
  cold = first render.
- **Chromium:** Google Chrome 149 via `puppeteer-core` (no separate download), with
  header/footer templates + `printBackground`. Cold = launch + navigate + first render;
  warm = render-only with the browser open.
- **Hardware:** local Apple Silicon (arm64), **not** the ODC Lambda runtime (x64).
  Absolute milliseconds will differ on ODC; the **relative** trends and the size/package
  facts hold. Chromium's local cold (1.9 s) is a best case — warm OS cache, no Lambda
  init; ODC's documented > 10 s is the realistic figure.
- **Reproduce:** `dotnet run -c Release --project samples/Benchmark -- <outDir> 600 12`,
  then `node render.js 12` against the emitted `benchmark.html`.
