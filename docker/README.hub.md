# GroupDocs.Comparison MCP Server

MCP server that exposes [GroupDocs.Comparison](https://products.groupdocs.com/comparison) as AI-callable tools for Claude, Cursor, GitHub Copilot, and other MCP agents.

## Quick start

```bash
docker run --rm -i \
  -v $(pwd)/documents:/data \
  groupdocs/comparison-net-mcp:latest
```

## Use with Claude Desktop

```json
{
  "mcpServers": {
    "groupdocs-comparison": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "-v", "/path/to/documents:/data", "groupdocs/comparison-net-mcp:latest"]
    }
  }
}
```

## Tools

- **Compare** — Compares two documents (source vs target) and produces a marked-up result **file**, plus a change-count summary and a structured JSON list of the changes (type, page, the changed fragment, surrounding source/target text, table cell, style changes). Use when the user wants the rendered diff document. Supports PDF, Word, Excel, PowerPoint, ODT, RTF, TXT, HTML, and 30+ more formats; optional `sourcePassword` / `targetPassword` cover protected documents.
- **AnalyzeChanges** — Returns the differences between two documents as **structured data only** — the same JSON change list as `Compare`, but **without** rendering or saving a result file. Cheaper than `Compare`; use when the user wants to summarize, explain, or reason about *what* changed rather than obtain the marked-up file. Optional `sourcePassword` / `targetPassword`.
- **GetDocumentInfo** — Inspects a single source document and returns file type, page count, file size, and per-page dimensions as JSON — without performing a comparison. Useful as a pre-flight check before deciding whether to compare or which formats to expect. Optional `password` for protected documents.

## Tags & environment

- Tags: `latest` + an immutable version tag per release matching NuGet (e.g. `26.7.1`).
  Platforms: `linux/amd64`, `linux/arm64`. Also on GHCR: `ghcr.io/groupdocs-comparison/comparison-net-mcp`.
- `GROUPDOCS_MCP_STORAGE_PATH` (default `/data`), `GROUPDOCS_MCP_OUTPUT_PATH` (optional),
  `GROUPDOCS_LICENSE_PATH` — mount your license and point at it to leave evaluation mode
  (see the Licensing section in the GitHub README for the exact evaluation limits).

Full docs, one-click installs for other clients, and licensing details:
**https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp**
