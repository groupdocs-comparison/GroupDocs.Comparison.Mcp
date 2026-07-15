using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupDocs.Comparison.Interfaces;
using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using ModelContextProtocol.Server;

namespace GroupDocs.Comparison.Mcp.Tools;

[McpServerToolType]
public static class GetDocumentInfoTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    [McpServerTool, Description(
        "Returns file type, page count, file size, and per-page dimensions for a source document — without performing a comparison. " +
        "Supports PDF, DOCX, XLSX, PPTX, ODT, RTF, TXT, HTML, and 30+ more document formats. " +
        "Call this tool whenever the user asks to inspect a document, get document info, check file type, page count, or properties before deciding whether to compare. " +
        "Do NOT pre-check whether files exist — just pass the filename the user provided. " +
        "The tool resolves files from storage and returns an error with available files if a name is not found. " +
        "Returns a JSON object with `fileName`, `fileType` (`fileFormat` + dot-prefixed `extension`, e.g. `.pdf`), `pageCount`, `sizeBytes`, and `pages` (per-page width/height). " +
        "On failure, the response text starts with 'Document-info lookup failed for' followed by the underlying exception type, message, and inner-exception chain.")]
    public static async Task<string> GetDocumentInfo(
        IFileResolver resolver,
        ILicenseManager licenseManager,
        FileInput file,
        [Description("Password for protected documents")] string? password = null)
    {
        licenseManager.SetLicense();
        using var resolved = await resolver.ResolveAsync(file);

        try
        {
            // Comparer accepts a stream-only constructor; we don't add a target
            // since we only inspect the source. Calling Source.GetDocumentInfo()
            // is the documented way to extract info without running a comparison.
            using var comparer = password != null
                ? new Comparer(resolved.Stream, new GroupDocs.Comparison.Options.LoadOptions { Password = password })
                : new Comparer(resolved.Stream);

            IDocumentInfo info = comparer.Source.GetDocumentInfo();

            var result = new
            {
                fileName = resolved.FileName,
                fileType = new
                {
                    fileFormat = info.FileType?.FileFormat,
                    extension = info.FileType?.Extension,
                },
                pageCount = info.PageCount,
                sizeBytes = info.Size,
                pages = info.PagesInfo?
                    .Select(p => new { pageNumber = p.PageNumber, width = p.Width, height = p.Height })
                    .ToList(),
            };

            // Raw JSON — never piped through OutputHelper.TruncateText (Pitfall #16).
            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (Exception ex)
        {
            // Surface the underlying engine exception instead of letting it
            // bubble to MCP's generic "An error occurred invoking
            // 'get_document_info'." wrapper. Pattern per Pitfall #18.
            return ToolError.Format("Document-info lookup", resolved.FileName, ex);
        }
    }
}
