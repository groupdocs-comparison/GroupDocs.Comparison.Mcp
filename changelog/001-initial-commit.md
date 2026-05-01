---
id: 001
date: 2026-05-01
version: 26.5.0
type: feature
---

# Initial public release of GroupDocs.Comparison MCP Server

## What changed
- NuGet package `GroupDocs.Comparison.Mcp` published with `McpServer` package type.
- Two MCP tools exposed:
  - `Compare` — compare two documents (source vs target), produce a marked-up result file with the differences highlighted, and return a change-count summary. Supports PDF, Word, Excel, PowerPoint, ODT, RTF, TXT, HTML, and 30+ more formats; takes optional `sourcePassword` / `targetPassword` for protected documents.
  - `GetDocumentInfo` — inspect a single source document and return file type, page count, file size, and per-page dimensions as JSON. No comparison performed. Optional `password` for protected documents.
- Installable via `dnx GroupDocs.Comparison.Mcp@26.5.0 --yes` (.NET 10 SDK required) or `dotnet tool install -g`.
- Docker image published to `ghcr.io/groupdocs-comparison/comparison-net-mcp` and `docker.io/groupdocs/comparison-net-mcp`.
- Environment variables: `GROUPDOCS_MCP_STORAGE_PATH`, optional `GROUPDOCS_MCP_OUTPUT_PATH`, `GROUPDOCS_LICENSE_PATH`.
- Linux native graphics deps wired up: `SkiaSharp.NativeAssets.Linux.NoDependencies` (3.119.0) is referenced because `GroupDocs.Comparison`'s nuspec already declares the SkiaSharp managed and native packages — we pin explicitly so transitive resolution stays deterministic. `libgdiplus` + `libfontconfig1` are installed in the Docker image and the `System.Drawing.EnableUnixSupport` runtime flag is set because Comparison's Cells (Excel) and image-format paths still call `System.Drawing.Common`.

## Why
Third product MCP server in the GroupDocs MCP framework (after Metadata and Conversion). Exposes
GroupDocs.Comparison for .NET as an AI-callable tool for Claude, Cursor,
VS Code / GitHub Copilot, and other MCP-compatible agents.

## Migration / impact
First release — no migration required.
