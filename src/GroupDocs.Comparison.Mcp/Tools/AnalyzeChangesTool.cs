using System.ComponentModel;
using System.Text;
using GroupDocs.Comparison.Options;
using GroupDocs.Comparison.Result;
using GroupDocs.Mcp.Core;
using GroupDocs.Mcp.Core.Licensing;
using ModelContextProtocol.Server;

namespace GroupDocs.Comparison.Mcp.Tools;

[McpServerToolType]
public static class AnalyzeChangesTool
{
    [McpServerTool, Description(
        "Analyzes the differences between two documents and returns them as structured data, WITHOUT producing a marked-up result file. " +
        "Supports PDF, DOCX, XLSX, PPTX, ODT, RTF, TXT, HTML, and 30+ more document formats. " +
        "Call this tool whenever the user wants to know WHAT changed between two files — to summarize, explain, list, or reason about the differences — rather than to obtain the rendered comparison document. " +
        "This is cheaper than `compare_documents` because it skips rendering and saving the result file; use `compare_documents` instead when the user needs that file to view, download, or share. " +
        "Do NOT pre-check whether files exist — just pass the filenames the user provided. " +
        "The tool resolves files from storage and returns an error with available files if a name is not found. " +
        "The returned text starts with either `<N> change(s) detected` or `No changes detected`, followed by a `Changes:` section containing a JSON array describing each change (type, component, page, the specific changed fragment `changedText`, surrounding source/target text, table cell, style changes). " +
        "On failure, the response text starts with 'Analyze failed for' followed by the underlying exception type, message, and inner-exception chain.")]
    public static async Task<string> AnalyzeChanges(
        IFileResolver resolver,
        ILicenseManager licenseManager,
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
            using var comparer = sourcePassword != null
                ? new Comparer(source.Stream, new LoadOptions { Password = sourcePassword })
                : new Comparer(source.Stream);

            comparer.Add(target.Stream, targetPassword != null
                ? new LoadOptions { Password = targetPassword }
                : new LoadOptions());

            // Parameterless Compare() runs the diff engine and populates the
            // change list but does NOT render or save a result document
            // (StreamChecker("") returns null, so SaveDocument is skipped). This
            // is the whole point of analyze_changes: pay only for the diff.
            var document = comparer.Compare();
            var changes = ResolveChanges(document, comparer);

            var summary = changes.Count > 0
                ? $"{changes.Count} change(s) detected"
                : "No changes detected";

            var prefix = licenseManager.IsLicensed ? string.Empty : "[Evaluation mode] Results may be limited.\n\n";
            var changesJson = ChangeProjector.Serialize(changes);
            return $"{prefix}Analyzed '{source.FileName}' vs '{target.FileName}' — {summary}\n\nChanges:\n{changesJson}";
        }
        catch (Exception ex)
        {
            // Surface the underlying engine exception (type + message + inner
            // chain) instead of MCP's generic "An error occurred invoking
            // 'analyze_changes'." wrapper. Pattern per Pitfall #18.
            return FormatException(ex, source.FileName, target.FileName);
        }
    }

    // Single-target Compare() returns the result Document whose Changes are
    // already populated; fall back to GetChanges() for any path that left the
    // returned document null.
    private static IReadOnlyList<ChangeInfo> ResolveChanges(Document? document, Comparer comparer)
    {
        if (document?.Changes != null && document.Changes.Count > 0)
            return document.Changes;

        return comparer.GetChanges();
    }

    private static string FormatException(Exception ex, string sourceName, string targetName)
    {
        var sb = new StringBuilder();
        sb.Append($"Analyze failed for '{sourceName}' vs '{targetName}': ");
        sb.Append($"{ex.GetType().FullName}: {ex.Message}");
        var inner = ex.InnerException;
        for (int depth = 0; inner != null && depth < 5; depth++, inner = inner.InnerException)
            sb.Append($" | inner({depth}): {inner.GetType().FullName}: {inner.Message}");
        return sb.ToString();
    }
}
