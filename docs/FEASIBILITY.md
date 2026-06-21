# Feasibility: QuestPDF as an ODC External Library

> Researched 2026-06-21 against current OutSystems docs, the official ODC SDK
> templates, QuestPDF docs/source, and QuestPDF Linux/container issue history.
> Confidence is noted per claim; the one genuinely unverified item is flagged.

## Verdict

**Yes — feasible, with one risk to validate empirically first.**

- The mechanism is proven: ODC external logic is a `dotnet publish` output zipped
  and run on **linux-x64**, and OutSystems' own **Ultimate PDF** ships a full
  native binary (Chromium) inside its external-logic package — so native assets
  can be shipped and loaded.
- Modern **QuestPDF (≥ 2024.3)** bundles its **own** native engine
  (`QuestPdfSkia.so`) inside the `QuestPDF` NuGet package. For `net8.0` the package
  has **no managed dependencies** — there is no SkiaSharp NuGet to fight.
- `byte[]` maps directly to OutSystems **Binary Data**, and `Document.GeneratePdf()`
  returns a `byte[]` fully in memory — no filesystem writes, which fits the
  read-only Lambda filesystem.

**Single biggest risk — unverified:** whether the ODC external-logic Linux
runtime's **glibc** satisfies what `QuestPdfSkia.so` requires (≈ GLIBC_2.28+).
There is **no public precedent** of QuestPDF running on ODC. A build that runs on
Windows/macOS can still throw `DllNotFoundException` / `"Your runtime is currently
not supported by QuestPDF"` / `GLIBC_2.xx not found` on the first ODC invocation,
and you **cannot** `apt-install` anything in that container. → Validate with a
hello-PDF spike before building out. If it fails on glibc, fall back to a fully
managed engine (e.g. iText — the proven ODC precedent, *AdvancedHtmlToPDF*).

## The key correction over naive guidance

Common advice says "add `SkiaSharp.NativeAssets.Linux.NoDependencies` +
`HarfBuzzSharp.NativeAssets.Linux`." **For current QuestPDF this is wrong.**
QuestPDF loads `QuestPdfSkia.so`, not `libSkiaSharp.so`; those packages do nothing
for its rendering and just add weight (toward the ~40 MB zip cap). Only add them
for legacy QuestPDF (≤ ~2024.2) or the optional SkiaSharp custom-graphics
integration. **Verify your pinned QuestPDF version shows "No dependencies" for
`net8.0` on nuget.org.**

## ODC External Libraries SDK (verified, high confidence)

- Package: `OutSystems.ExternalLibraries.SDK` **1.5.0** (no deps).
- Target: **net8.0** (templates also list net10.0; .NET 6 was the original target).
- Attributes: `[OSInterface]` (one only), `[OSAction]`, `[OSParameter]`,
  `[OSStructure]`, `[OSStructureField]`, `[OSIgnore]`; types via `OSDataType`.
- Type mapping: `string→Text`, `int→Integer`, `long→Long Integer`, `bool→Boolean`,
  `decimal/float/double→Decimal`, `DateTime→Date Time`, **`byte[]→Binary Data`**;
  `[OSStructure]`→Structure; `IEnumerable<T>`→List. Omitting `DataType` infers from
  the .NET type.
- One public class with a public parameterless constructor implements the interface.
- Package: `dotnet publish -c Release -r linux-x64 --self-contained false -f net8.0`,
  then zip the publish folder contents (DLLs flat + QuestPDF's native engine
  `libQuestPdfSkia.so`/`libqpdf.so` at the publish root + bundled `LatoFont` +
  `*.deps.json`). Transitive NuGet deps are bundled automatically by publish.
  Verified: the resulting zip is ~9.7 MB.

## Runtime constraints (verified; runtime substrate medium confidence)

- Runs on AWS Lambda-style infra, **.NET 8 / Linux / x64** (linux-x64 hard-confirmed
  from the official publish script; the AL2023/glibc specifics are inferred).
- **5.5 MB** combined input+output payload ceiling → store-via-REST for large PDFs.
- **95 s** timeout + cold-start latency (> 10 s first call).
- **~40 MB** external-library ZIP cap.
- Read-only filesystem except ephemeral **/tmp** (~512 MB) — PDF-to-`byte[]` needs
  no writes.
- Outbound network is available (via NAT); external logic is outside the app's
  network scope, so use **token auth, not IP filtering** for callbacks.

## QuestPDF (verified, high confidence)

- Package `QuestPDF` (CalVer; current ≈ 2026.6.0). Multi-targets
  net10/net8/net6/netstandard2.0.
- License set once at startup: `QuestPDF.Settings.License = LicenseType.Community;`
  (honour-system enum, no key, no network). Community free under **USD 1M** revenue.
- Fluent API: `Document.Create(c => c.Page(p => { p.Header()/.Content()/.Footer() }))`
  then `.GeneratePdf()` → `byte[]` / `(Stream)` / `(path)`.
- Bundles **Lato** by default → text always renders even with zero system fonts;
  a missing custom font **silently falls back to Lato** (no error). Register fonts
  via `FontManager.RegisterFont(stream)`; set `UseEnvironmentFonts = false` for
  deterministic, host-independent output.

## Key sources

- ODC custom code: <https://success.outsystems.com/documentation/outsystems_developer_cloud/building_apps/extend_your_apps_with_custom_code/>
- SDK reference: <https://success.outsystems.com/documentation/outsystems_developer_cloud/building_apps/extend_your_apps_with_custom_code/external_libraries_sdk_reference/>
- SDK package: <https://www.nuget.org/packages/OutSystems.ExternalLibraries.SDK>
- SDK templates: <https://github.com/OutSystems/OutSystems.ExternalLibraries.SDK-templates>
- Native-binary precedent (Ultimate PDF): <https://github.com/OutSystems/UltimatePDF-ExternalLogic>
- Managed-PDF precedent (iText, ODC): <https://www.outsystems.com/forge/component-overview/21526/advancedhtmltopdf-odc>
- QuestPDF: <https://github.com/QuestPDF/QuestPDF> · <https://www.questpdf.com/license/configuration.html> · <https://www.questpdf.com/api-reference/text/font-management.html>
- QuestPDF native/Skia integration note: <https://www.questpdf.com/api-reference/skiasharp-integration.html>
- QuestPDF self-bundled native (no SkiaSharp NuGet): <https://github.com/QuestPDF/QuestPDF/discussions/622>
- Linux native load failures: <https://github.com/QuestPDF/QuestPDF/issues/700> · <https://github.com/QuestPDF/QuestPDF/issues/15>
