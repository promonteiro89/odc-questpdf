# Fonts

Drop `.ttf` / `.otf` files in this folder to embed them in the library. They are
picked up automatically by the `<EmbeddedResource Include="Resources/Fonts/*.ttf" />`
glob in the `.csproj` and registered at startup by `FontBootstrapper`.

Each font becomes usable by its **embedded family name** — set it via
`DocumentOptions.FontFamily` (or it falls back to `"Lato"`).

## Why this matters on ODC

The ODC external-logic container has **no system fonts** and you cannot install
any. QuestPDF ships the **Lato** font, so text always renders — but if you rely
on a font that isn't embedded, output **silently falls back to Lato** (no error).
If you need a specific corporate font, or non-Latin scripts (CJK, Arabic, etc.),
add the font file here.

Use fonts you are licensed to embed/redistribute (e.g. SIL OFL fonts such as
the Noto family).
