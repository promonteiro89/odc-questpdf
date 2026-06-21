namespace OutSystems.QuestPdf;

/// <summary>
/// Fully-qualified embedded-resource names for the icons shown in the ODC editor.
/// Format is &lt;RootNamespace&gt;.&lt;folder&gt;.&lt;file&gt;. The PNGs live in Resources/Icons
/// and are embedded by the *.png glob in the .csproj.
/// Replace app.png / action.png with your own 512×512 PNGs to rebrand.
/// </summary>
internal static class IconNames
{
    public const string App = "OutSystems.QuestPdf.Resources.Icons.app.png";
    public const string Action = "OutSystems.QuestPdf.Resources.Icons.action.png";
}
