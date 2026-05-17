using System.ComponentModel;
using System.Text;
using GroupDocs.Comparison.Options;
using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using ModelContextProtocol.Server;

namespace GroupDocs.Comparison.Mcp.Tools;

[McpServerToolType]
public static class CompareTool
{
    [McpServerTool, Description(
        "Compares two documents and highlights the differences between them. " +
        "Supports PDF, DOCX, XLSX, PPTX, ODT, RTF, TXT, HTML, and 30+ more document formats. " +
        "Call this tool immediately whenever the user asks to compare, diff, or check differences between two files. " +
        "Do NOT pre-check whether files exist — just pass the filenames the user provided. " +
        "The tool resolves files from storage and returns an error with available files if a name is not found. " +
        "The returned text includes either `<N> change(s) detected` or `No changes detected`, followed by the saved path of the marked-up result document (file name pattern: `<source-stem>_compared<source-ext>`). " +
        "On failure, the response text starts with 'Compare failed for' followed by the underlying exception type, message, and inner-exception chain.")]
    public static async Task<string> Compare(
        IFileResolver resolver,
        IFileStorage storage,
        ILicenseManager licenseManager,
        OutputHelper output,
        [Description("Source (original) document — provide the filename as given by the user, e.g. 'source.pdf'")] FileInput sourceFile,
        [Description("Target (modified) document to compare against — provide the filename as given by the user, e.g. 'target.pdf'")] FileInput targetFile,
        [Description("Password for source document, if password-protected")] string? sourcePassword = null,
        [Description("Password for target document, if password-protected")] string? targetPassword = null)
    {
        licenseManager.SetLicense();
        using var source = await resolver.ResolveAsync(sourceFile);
        using var target = await resolver.ResolveAsync(targetFile);

        try
        {
            var outputName = $"{Path.GetFileNameWithoutExtension(source.FileName)}_compared{Path.GetExtension(source.FileName)}";

            using var outputMs = new MemoryStream();
            using var comparer = sourcePassword != null
                ? new Comparer(source.Stream, new LoadOptions { Password = sourcePassword })
                : new Comparer(source.Stream);

            comparer.Add(target.Stream, targetPassword != null
                ? new LoadOptions { Password = targetPassword }
                : new LoadOptions());

            comparer.Compare(outputMs);

            var changes = comparer.GetChanges();
            var summary = changes.Length > 0
                ? $"{changes.Length} change(s) detected"
                : "No changes detected";

            var savedPath = await storage.WriteFileAsync(outputName, outputMs.ToArray(), rewrite: false);

            var prefix = licenseManager.IsLicensed ? string.Empty : "[Evaluation mode] Output may include watermarks.\n\n";
            var description = $"{prefix}Compared '{source.FileName}' vs '{target.FileName}' — {summary}";
            return await output.BuildFileOutputAsync(savedPath, description);
        }
        catch (Exception ex)
        {
            // Surface the underlying engine exception (type + message + inner
            // chain) instead of letting it bubble to ModelContextProtocol's
            // generic "An error occurred invoking 'compare'." wrapper, which
            // hides the real cause and makes native-deps / fixture issues
            // indistinguishable. Pattern per Pitfall #18 of the clone prompt.
            return FormatException(ex, source.FileName, target.FileName);
        }
    }

    private static string FormatException(Exception ex, string sourceName, string targetName)
    {
        var sb = new StringBuilder();
        sb.Append($"Compare failed for '{sourceName}' vs '{targetName}': ");
        sb.Append($"{ex.GetType().FullName}: {ex.Message}");
        var inner = ex.InnerException;
        for (int depth = 0; inner != null && depth < 5; depth++, inner = inner.InnerException)
            sb.Append($" | inner({depth}): {inner.GetType().FullName}: {inner.Message}");
        return sb.ToString();
    }
}
