using System;
using System.Net.Http;
using System.Net.Http.Headers;
using OutSystems.QuestPdf.Models;

namespace OutSystems.QuestPdf.Internal;

// POSTs the rendered PDF to a callback URL (RenderAndStore), bypassing the 5.5 MB
// inline-payload limit.
internal static class PdfDelivery
{
    // Shared client; timeout under the ODC 95 s external-logic limit.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(80) };

    public static UploadResult Post(byte[] pdf, string url, string? bearerToken, string fileName)
    {
        try
        {
            using var content = new ByteArrayContent(pdf);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };

            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (!string.IsNullOrWhiteSpace(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = Http.Send(request);

            return new UploadResult
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                SizeBytes = pdf.LongLength,
                Error = response.IsSuccessStatusCode ? string.Empty : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
            };
        }
        catch (Exception ex)
        {
            return new UploadResult
            {
                Success = false,
                StatusCode = 0,
                SizeBytes = pdf?.LongLength ?? 0,
                Error = ex.Message,
            };
        }
    }
}
