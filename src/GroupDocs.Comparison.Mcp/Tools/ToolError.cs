using System.Text;

namespace GroupDocs.Comparison.Mcp.Tools;

// Shared descriptive-error formatter for the tool surface. Engine failures are
// surfaced as text (not ModelContextProtocol's opaque "An error occurred
// invoking '<tool>'") so AI agents and integration tests can read the cause.
// The text always starts with "<op> failed for '<file>'<subjectSuffix>: ..." —
// integration tests match that prefix (Pitfall #18).
internal static class ToolError
{
    // subjectSuffix lets multi-input tools name the second subject, e.g. the
    // Compare tool passes " vs '<target>'" so the message reads
    // "Compare failed for '<source>' vs '<target>': ...".
    public static string Format(string op, string file, Exception ex, string? subjectSuffix = null)
    {
        var sb = new StringBuilder();
        sb.Append($"{op} failed for '{file}'{subjectSuffix}: {ex.GetType().FullName}: {ex.Message}");
        var inner = ex.InnerException;
        for (int d = 0; inner != null && d < 5; d++, inner = inner.InnerException)
        {
            sb.Append($" | inner({d}): {inner.GetType().FullName}: {inner.Message}");
        }
        return sb.ToString();
    }
}
