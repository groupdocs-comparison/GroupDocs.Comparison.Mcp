using System.Text.Json;
using System.Text.Json.Serialization;
using GroupDocs.Comparison.Result;

namespace GroupDocs.Comparison.Mcp.Tools;

/// <summary>
/// LLM-facing projection of a single <see cref="ChangeInfo"/>.
///
/// The library's <see cref="ChangeInfo"/> carries data that is only useful to a
/// human looking at the rendered document (pixel-level <c>Box</c> coordinates,
/// page width/height, accept/reject <c>ComparisonAction</c>, internal node
/// positions). An MCP client is an LLM that reads text, so we keep only the
/// fields it can reason about: what changed, of what kind, where (page), and —
/// for tabular formats — which cell. Style changes are included because they
/// carry meaningful semantics (font, bold, color, …) the model may be asked
/// about. Empty/null fields are omitted so the serialized output stays compact.
/// </summary>
public sealed class ChangeProjection
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Change kind: Inserted, Deleted, Modified, StyleChanged, etc.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Kind of element that changed (text, image, cell, …).</summary>
    [JsonPropertyName("componentType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ComponentType { get; set; }

    /// <summary>
    /// The specific changed fragment itself — the inserted/deleted run of text
    /// (e.g. the word "Microsoft"), as opposed to the surrounding paragraph in
    /// <see cref="SourceText"/>/<see cref="TargetText"/>. Omitted when it would
    /// merely duplicate the source or target text.
    /// </summary>
    [JsonPropertyName("changedText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChangedText { get; set; }

    /// <summary>1-based page number the change is located on, when available.</summary>
    [JsonPropertyName("page")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Page { get; set; }

    /// <summary>Text as it was in the source (original) document.</summary>
    [JsonPropertyName("sourceText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceText { get; set; }

    /// <summary>Text as it is in the target (modified) document.</summary>
    [JsonPropertyName("targetText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetText { get; set; }

    /// <summary>Zero-based row index for tabular (Cells) comparisons.</summary>
    [JsonPropertyName("row")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Row { get; set; }

    /// <summary>Zero-based column index for tabular (Cells) comparisons.</summary>
    [JsonPropertyName("column")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Column { get; set; }

    /// <summary>Column header text for tabular (Cells) comparisons.</summary>
    [JsonPropertyName("columnHeader")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColumnHeader { get; set; }

    /// <summary>Per-property formatting changes (font, bold, color, …).</summary>
    [JsonPropertyName("styleChanges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<StyleChangeProjection>? StyleChanges { get; set; }
}

/// <summary>
/// LLM-facing projection of a single <see cref="StyleChangeInfo"/>.
/// </summary>
public sealed class StyleChangeProjection
{
    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("oldValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OldValue { get; set; }

    [JsonPropertyName("newValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NewValue { get; set; }
}

/// <summary>
/// Maps the library's <see cref="ChangeInfo"/> model to the compact,
/// LLM-friendly <see cref="ChangeProjection"/> and serializes it to JSON.
/// </summary>
public static class ChangeProjector
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    /// <summary>
    /// Projects a list of changes to compact DTOs, dropping fields an LLM
    /// cannot use (pixel boxes, page dimensions, accept/reject action).
    /// </summary>
    public static IReadOnlyList<ChangeProjection> Project(IEnumerable<ChangeInfo> changes)
    {
        var result = new List<ChangeProjection>();
        if (changes == null)
            return result;

        foreach (var change in changes)
        {
            if (change == null)
                continue;

            var sourceText = NullIfEmpty(change.SourceText);
            var targetText = NullIfEmpty(change.TargetText);

            result.Add(new ChangeProjection
            {
                Id = change.Id,
                Type = change.Type.ToString(),
                ComponentType = NullIfEmpty(change.ComponentType),
                ChangedText = ProjectChangedText(change.Text, sourceText, targetText),
                Page = change.PageInfo?.PageNumber,
                SourceText = sourceText,
                TargetText = targetText,
                Row = change.Row,
                Column = change.Column,
                ColumnHeader = NullIfEmpty(change.ColumnHeader),
                StyleChanges = ProjectStyleChanges(change.StyleChanges),
            });
        }

        return result;
    }

    /// <summary>
    /// Serializes a projected change list to indented JSON.
    /// </summary>
    public static string Serialize(IEnumerable<ChangeInfo> changes)
        => JsonSerializer.Serialize(Project(changes), JsonOptions);

    private static List<StyleChangeProjection>? ProjectStyleChanges(StyleChangeInfo[]? styleChanges)
    {
        if (styleChanges == null || styleChanges.Length == 0)
            return null;

        var projected = new List<StyleChangeProjection>(styleChanges.Length);
        foreach (var style in styleChanges)
        {
            if (style == null)
                continue;

            projected.Add(new StyleChangeProjection
            {
                Property = style.PropertyName ?? string.Empty,
                OldValue = NullIfEmpty(style.OldValue?.ToString()),
                NewValue = NullIfEmpty(style.NewValue?.ToString()),
            });
        }

        return projected.Count > 0 ? projected : null;
    }

    /// <summary>
    /// Keeps <see cref="ChangeInfo.Text"/> only when it adds information beyond
    /// the paragraph-level source/target text. For paragraph-level inserts,
    /// deletes and style changes the engine sets Text equal to source or target,
    /// so we drop it to avoid duplicating the same string three times.
    /// </summary>
    private static string? ProjectChangedText(string? text, string? sourceText, string? targetText)
    {
        var changed = NullIfEmpty(text);
        if (changed == null)
            return null;

        if (changed == sourceText || changed == targetText)
            return null;

        return changed;
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrEmpty(value) ? null : value;
}
