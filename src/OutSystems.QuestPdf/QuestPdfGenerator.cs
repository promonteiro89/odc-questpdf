using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using OutSystems.QuestPdf.Internal;
using OutSystems.QuestPdf.Models;
using OutSystems.QuestPdf.Rendering;
using LicenseType = QuestPDF.Infrastructure.LicenseType;

namespace OutSystems.QuestPdf;

// One public class implementing the single [OSInterface], with a public
// parameterless constructor, as ODC requires.
public sealed class QuestPdfGenerator : IQuestPdfGenerator
{
    // QuestPDF Community is free only under USD 1M annual revenue; above that set
    // Professional/Enterprise (honour-system enum, a licensing decision).
    private const LicenseType LicenseTier = LicenseType.Community;

    private static readonly object InitGate = new();
    private static bool _initialized;

    private readonly ILogger? _logger;

    public QuestPdfGenerator() : this(null) { }

    // ODC injects an ILogger here (Custom Code Logging & Tracing); null-safe locally.
    public QuestPdfGenerator(ILogger? logger)
    {
        _logger = logger;
        EnsureInitialized();
    }

    internal static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (InitGate)
        {
            if (_initialized) return;
            QuestPDF.Settings.License = LicenseTier;
            QuestPDF.Settings.UseEnvironmentFonts = false; // don't depend on host fonts
            FontBootstrapper.RegisterEmbeddedFonts();
            _initialized = true;
        }
    }

    public byte[] Render(DocumentOptions options, IEnumerable<Block> content)
    {
        EnsureInitialized();
        var spec = BlockCompiler.Compile(options, content);
        return RenderInternal(spec, nameof(Render));
    }

    public UploadResult RenderAndStore(DocumentOptions options, IEnumerable<Block> content, string callbackUrl, string authToken)
    {
        EnsureInitialized();
        var spec = BlockCompiler.Compile(options, content);
        var pdf = RenderInternal(spec, nameof(RenderAndStore));
        var result = PdfDelivery.Post(pdf, callbackUrl, authToken, BuildFileName(spec.Title));
        if (result.Success)
            _logger?.LogInformation("QuestPdf stored PDF: {Bytes} bytes, HTTP {Status}", result.SizeBytes, result.StatusCode);
        else
            _logger?.LogWarning("QuestPdf store failed: HTTP {Status} {Error}", result.StatusCode, result.Error);
        return result;
    }

    private byte[] RenderInternal(DocumentSpec spec, string operation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var pdf = new PdfComposer(spec).Render();
            sw.Stop();
            _logger?.LogInformation("QuestPdf {Operation} succeeded: {Bytes} bytes in {ElapsedMs} ms", operation, pdf.Length, sw.ElapsedMilliseconds);
            return pdf;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger?.LogError(ex, "QuestPdf {Operation} failed after {ElapsedMs} ms: {Message}", operation, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private static string BuildFileName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "document.pdf";
        var safe = new string(title!.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray()).Trim('_');
        return safe.Length > 0 ? safe + ".pdf" : "document.pdf";
    }
}
