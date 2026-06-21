using System.Collections.Generic;
using OutSystems.ExternalLibraries.SDK;
using OutSystems.QuestPdf.Models;

namespace OutSystems.QuestPdf;

[OSInterface(
    Name = "QuestPDF",
    Description = "Render a PDF in one call from page options and a list of content blocks.",
    IconResourceName = IconNames.App)]
public interface IQuestPdfGenerator
{
    [OSAction(
        IconResourceName = IconNames.Action,
        Description = "Render a PDF in ONE call from page options and an ordered list of content blocks (build it with ListAppend: Heading, Text, RichText, List, Image, Svg, Table, Divider, Space, PageBreak, BeginRow/EndRow, BeginCell/EndCell, BeginColumn/EndColumn, BeginBox/EndBox, BeginSection/EndSection, Toc). Returns Binary Data.",
        ReturnName = "Pdf")]
    byte[] Render(
        [OSParameter(Description = "Page setup, default text, metadata, header/footer, watermark and fonts.")]
        DocumentOptions options,
        [OSParameter(Description = "Ordered content blocks. Build the list with ListAppend.")]
        IEnumerable<Block> content);

    [OSAction(
        IconResourceName = IconNames.Action,
        Description = "Render the PDF and POST it to a callback URL (your REST endpoint or a pre-signed S3 URL), returning a small status. Use for PDFs over the 5.5 MB inline payload limit.",
        ReturnName = "Result")]
    UploadResult RenderAndStore(
        [OSParameter(Description = "Page setup, default text, metadata, header/footer, watermark and fonts.")]
        DocumentOptions options,
        [OSParameter(Description = "Ordered content blocks. Build the list with ListAppend.")]
        IEnumerable<Block> content,
        [OSParameter(DataType = OSDataType.Text, Description = "HTTPS URL the PDF bytes are POSTed to.")]
        string callbackUrl,
        [OSParameter(DataType = OSDataType.Text, Description = "Optional bearer token (token auth, not IP filtering).")]
        string authToken);
}
