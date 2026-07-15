using System.ComponentModel;
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
        "Use this tool when the user wants the marked-up result FILE saved (to view, download, or share). " +
        "If the user only wants to know WHAT changed (an analysis or summary of the differences) without needing the rendered file, prefer the `analyze_changes` tool, which is cheaper because it skips rendering. " +
        "The returned text includes either `<N> change(s) detected` or `No changes detected`, followed by the saved path of the marked-up result document (file name pattern: `<source-stem>_compared<source-ext>`), " +
        "and then a `Changes:` section containing a JSON array describing each change (type, component, page, the specific changed fragment `changedText`, surrounding source/target text, table cell, style changes). " +
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
            var fileOutput = await output.BuildFileOutputAsync(savedPath, description);

            // Surface the structured changes alongside the rendered file. The
            // file is for a human to open; this JSON is what the calling LLM can
            // actually read to answer "what changed?". Raw JSON is appended
            // verbatim and never piped through OutputHelper.TruncateText.
            var changesJson = ChangeProjector.Serialize(changes);
            return $"{fileOutput}\n\nChanges:\n{changesJson}";
        }
        catch (Exception ex)
        {
            // Surface the underlying engine exception (type + message + inner
            // chain) instead of letting it bubble to ModelContextProtocol's
            // generic "An error occurred invoking 'compare'." wrapper, which
            // hides the real cause and makes native-deps / fixture issues
            // indistinguishable. Pattern per Pitfall #18 of the clone prompt.
            return ToolError.Format("Compare", source.FileName, ex, $" vs '{target.FileName}'");
        }
    }
}
