# GroupDocs.Comparison MCP Server

MCP server that exposes [GroupDocs.Comparison](https://products.groupdocs.com/comparison) as AI-callable tools
for Claude, Cursor, GitHub Copilot, and other MCP agents.

## Installation

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

**Run directly with `dnx` (recommended — no install step):**

```bash
dnx GroupDocs.Comparison.Mcp --yes
```

Pulls the latest stable release on every invocation. To pin to a specific
version (recommended for shared configs and CI), append `@<version>`:

```bash
dnx GroupDocs.Comparison.Mcp@26.5.0 --yes
```

**Or install as a global dotnet tool:**

```bash
dotnet tool install -g GroupDocs.Comparison.Mcp
groupdocs-comparison-mcp
```

**Or run via Docker:**

```bash
docker run --rm -i \
  -v $(pwd)/documents:/data \
  ghcr.io/groupdocs-comparison/comparison-net-mcp:latest
```

## Available MCP Tools

| Tool | Description |
|---|---|
| `Compare` | Compares two documents (source vs target) and produces a marked-up result file plus a change-count summary. Supports PDF, Word, Excel, PowerPoint, ODT, RTF, TXT, HTML, and 30+ more formats; optional `sourcePassword` / `targetPassword` cover protected documents. |
| `GetDocumentInfo` | Inspects a single source document and returns file type, page count, file size, and per-page dimensions as JSON — without performing a comparison. Useful as a pre-flight check before deciding whether to compare or which formats to expect. Optional `password` for protected documents. |

## Example prompts for AI agents

Once the server is wired up to your MCP client (Claude Desktop, Cursor, VS Code Copilot, …), try:

```
Compare old.pdf and new.pdf — what changed?

Diff contract-v1.docx against contract-v2.docx and tell me the change count.

Show the differences between budget-q1.xlsx and budget-q2.xlsx.

How many pages does report.pdf have? Who's the author?

Inspect /docs/legal-brief.pdf — what's the file type and page count?
```

The client picks `Compare` for diff questions and `GetDocumentInfo` for
inspection-only questions.

## Configuration

| Variable | Description | Default |
|---|---|---|
| `GROUPDOCS_MCP_STORAGE_PATH` | Base folder for input and output files | current directory |
| `GROUPDOCS_MCP_OUTPUT_PATH` | *(Optional)* separate folder for output files | `GROUPDOCS_MCP_STORAGE_PATH` |
| `GROUPDOCS_LICENSE_PATH` | Path to GroupDocs license file | (evaluation mode) |

## Usage with Claude Desktop

```json
{
  "mcpServers": {
    "groupdocs-comparison": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Comparison.Mcp", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "/path/to/documents"
      }
    }
  }
}
```

> To pin to a specific version, replace `"GroupDocs.Comparison.Mcp"` with
> `"GroupDocs.Comparison.Mcp@26.5.0"` in `args`. Pinning is recommended for
> shared / committed configs to avoid surprise upgrades.

## Usage with VS Code / GitHub Copilot

NuGet.org generates a ready-to-use `mcp.json` snippet on the [package page](https://www.nuget.org/packages/GroupDocs.Comparison.Mcp).
Copy it directly into your `.vscode/mcp.json`.

Alternatively, add manually to `.vscode/mcp.json`:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "storage_path",
      "description": "Base folder for input and output files.",
      "password": false
    }
  ],
  "servers": {
    "groupdocs-comparison": {
      "type": "stdio",
      "command": "dnx",
      "args": ["GroupDocs.Comparison.Mcp", "--yes"],
      "env": {
        "GROUPDOCS_MCP_STORAGE_PATH": "${input:storage_path}"
      }
    }
  }
}
```

> Same pinning rule as above — swap `"GroupDocs.Comparison.Mcp"` for
> `"GroupDocs.Comparison.Mcp@26.5.0"` to lock to a specific release.

## Usage with Docker Compose

```bash
cd docker
docker compose up
```

Edit `docker/docker-compose.yml` to point volumes at your local documents folder.

## Documentation & guides

Step-by-step deployment guides and a published-package integration test suite
live in the companion repo
[**GroupDocs.Comparison.Mcp.Tests**](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests):

- [Install from NuGet](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests/blob/master/how-to/01-install-from-nuget.md) — `dnx`, global tool, pinned vs always-latest
- [Run via Docker](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests/blob/master/how-to/02-run-via-docker.md)
- [Verify on the MCP registry](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests/blob/master/how-to/03-verify-mcp-registry.md)
- [Use with Claude Desktop](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests/blob/master/how-to/04-use-with-claude-desktop.md)
- [Use with VS Code / GitHub Copilot](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests/blob/master/how-to/05-use-with-vscode-copilot.md)
- [Run the integration tests](https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests/blob/master/how-to/06-run-integration-tests.md)

That repo also exercises every advertised tool against the **published** NuGet
artifact on Linux, macOS, and Windows in CI — so the snippets above are
verified end-to-end on every release.

## License

MIT — see [LICENSE](LICENSE)

<!-- mcp-name: io.github.groupdocs-comparison/groupdocs-comparison-mcp -->
