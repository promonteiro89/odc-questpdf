# RenderJson — document JSON schema

`RenderJson(documentJson)` renders a whole document tree in one call. Property names
are **case-insensitive**. The handle returned by the chained builder is exactly this
JSON, so you can also capture a handle and feed its text straight into `RenderJson`.

## Top level

```jsonc
{
  "options":   { /* page setup */ },
  "title":     "string",     // PDF metadata (optional)
  "author":    "string",
  "subject":   "string",
  "header":    { /* header/footer object */ },
  "footer":    { /* header/footer object */ },
  "watermark": { "text": "DRAFT", "style": { /* style */ } },
  "fonts":     [ { "name": "Brand", "data": "<base64 TTF/OTF>" },
                 { "name": "BrandWeb", "url": "https://cdn.example.com/brand.ttf" } ],
  "root":      { "type": "Column", "children": [ /* nodes */ ] }
}
```

Only `root` is required. `root` should be a `Column` (or any container) holding the
document body.

### options
| Field | Type | Default |
|---|---|---|
| `pageSize` | "A4" \| "A3" \| "A5" \| "Letter" \| "Legal" | A4 |
| `landscape` | bool | false |
| `marginCm` | number | 2 |
| `fontFamily` | string | "Lato" |
| `fontSize` | number (pt) | 11 |

### header / footer
| Field | Type |
|---|---|
| `text` | string |
| `style` | style object |
| `showPageNumbers` | bool (footer only — adds `page X / Y`) |

## Nodes

Every node has a `type`. Container nodes have `children` (an array of nodes).

| `type` | Fields used | Notes |
|---|---|---|
| `Column` | `children`, `number` | Vertical stack. `number` = item spacing (pt). |
| `Row` | `children`, `number` | Side-by-side. Direct children become equal columns unless they are `Cell`s. `number` = column spacing. |
| `Cell` | `children`, `widthType`, `width` | A sized column inside a `Row`. `widthType`: "Relative" (weight) or "Constant" (points). |
| `Box` | `children`, `box` | Decorated container (background/border/padding). |
| `Text` | `text`, `style` | Newlines in `text` become line breaks. |
| `RichText` | `spans`, `align` | Mixed runs in one paragraph. `align`: Left/Center/Right/Justify. |
| `Heading` | `text`, `level`, `style` | `level` 1–3 sets default size unless `style.fontSize` is set. |
| `List` | `items` (string[]), `ordered` (bool), `style` | Bulleted or numbered. |
| `Image` | `image` (base64), `fit`, `maxWidth`, `align` | `fit`: FitWidth/FitArea/Original. |
| `Svg` | `svg` (string), `fit`, `maxWidth`, `align` | Inline SVG markup. |
| `Table` | `table` | See table object. |
| `Section` | `name`, `children` | A named navigation target that may span pages (link to it from a TOC or section link). |
| `Toc` | `toc`, `style` | Clickable table of contents. `toc` = array of `{ label, sectionName, indent }`. |
| `Divider` | `colorHex`, `number` | `number` = line thickness (pt). |
| `Space` | `number` | `number` = height (pt). |
| `PageBreak` | — | Forces a new page. |

> `number` is reused by node type: spacing (Column/Row), thickness (Divider), height (Space).

### style object
| Field | Type |
|---|---|
| `fontFamily` | string |
| `fontSize` | number (pt) |
| `bold`, `italic`, `underline`, `strike`, `super`, `sub` | bool |
| `colorHex` | "#RRGGBB" or "#AARRGGBB" |
| `bgHex` | hex highlight behind text |
| `align` | Left/Center/Right/Justify |
| `lineHeight` | number (multiplier) |

### span object (for RichText)
`{ "text": "...", "style": { ... }, "hyperlink": "https://...", "sectionLink": "sectionName" }`
(`hyperlink` = external URL; `sectionLink` = jump to a `Section` by name.)

### table object
| Field | Type |
|---|---|
| `columns` | string[] (header texts; omit for no header) |
| `columnWidths` | number[] (relative; omit for equal) |
| `rows` | row[] |
| `headerStyle`, `bodyStyle` | style |
| `headerBackgroundHex` | hex |
| `showBorders` | bool |

**row**: `{ "cells": ["a","b"] }` (plain) or `{ "richCells": [ cell, ... ] }` (styled).
**cell**: `{ "text": "...", "style": {...}, "backgroundHex": "#...", "align": "Right", "columnSpan": 2 }`.

### box object
`{ "backgroundHex": "#eef4fb", "borderColorHex": "#1f3b57", "borderThickness": 1, "padding": 10 }`

## Notes
- **Images**: `image` is a base64 string of the PNG/JPEG bytes.
- **Colors** must be valid hex (`#RRGGBB`/`#AARRGGBB`) or rendering fails with a clear
  error naming the field.
- This expresses any combination of primitives at any depth. The only QuestPDF
  capability it cannot reach is render-time C# *logic*; for arbitrary vector content
  use an `Svg` node.
