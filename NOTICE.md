# Third-party notices

This library is MIT-licensed (see `LICENSE`), but it depends on and ships
third-party components under their own licenses.

| Component | License | Note |
|---|---|---|
| **QuestPDF** | Community / Professional / Enterprise (dual-licensed) | **Free under the Community License only for organisations below USD 1,000,000 annual revenue.** Above that, a paid license is required. Set `QuestPdfGenerator.LicenseTier` accordingly. Using this wrapper does **not** grant you a QuestPDF license. <https://www.questpdf.com/license/> |
| **Lato font** (bundled by QuestPDF) | SIL Open Font License 1.1 | The default font; used as a fallback when no custom font is registered. |
| **OutSystems.ExternalLibraries.SDK** | BSD-3-Clause | The ODC External Logic attributes. |
| **Microsoft.Extensions.Logging.Abstractions** | MIT | `ILogger` abstractions for ODC observability. |
| **QuestPDF native engine** (`libQuestPdfSkia.so`, `libqpdf.so`) | Bundled within the QuestPDF package | Built on Skia/HarfBuzz; covered by the QuestPDF package licensing. |

The icons in `src/OutSystems.QuestPdf/Resources/Icons/` are project brand assets.
