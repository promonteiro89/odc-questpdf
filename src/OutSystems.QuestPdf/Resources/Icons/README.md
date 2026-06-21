# Icons

These PNGs are embedded (via the `Resources/Icons/*.png` glob) and wired to the
ODC editor through `IconResourceName` (see `IconNames.cs`):

| File | Used for | Attribute |
|---|---|---|
| `app.png` | The library icon | `[OSInterface(IconResourceName = IconNames.App)]` |
| `action.png` | Every server action | `[OSAction(IconResourceName = IconNames.Action)]` |

**To rebrand:** replace `app.png` and `action.png` with your own **512×512 PNG**
files (same names) and rebuild. No code change needed.

> The committed files are clean placeholders generated for this project. Drop in your
> exact brand assets to override them.
