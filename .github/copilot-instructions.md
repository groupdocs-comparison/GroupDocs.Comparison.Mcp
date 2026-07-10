# GitHub Copilot instructions — GroupDocs.Comparison MCP Server

This repository is a standalone **MCP (Model Context Protocol) server** that exposes [GroupDocs.Comparison for .NET](https://products.groupdocs.com/comparison) as AI-callable tools for Claude, Cursor, GitHub Copilot, and other MCP clients.

Published to NuGet as `GroupDocs.Comparison.Mcp` (`PackageType=McpServer`) and as container images at `ghcr.io/groupdocs-comparison/comparison-net-mcp` and `docker.io/groupdocs/comparison-net-mcp`. MIT-licensed.

## Run / install

```bash
# Run directly with dnx (recommended — no install step); pin with @<version> for shared configs/CI
dnx GroupDocs.Comparison.Mcp --yes

# Or install as a global dotnet tool
dotnet tool install -g GroupDocs.Comparison.Mcp
groupdocs-comparison-mcp

# Or run via Docker
docker run --rm -i -v $(pwd)/documents:/data ghcr.io/groupdocs-comparison/comparison-net-mcp:latest
```

## MCP tools exposed (3)

- **Compare** — compare two documents (source vs target), produce a marked-up result **file** with differences highlighted, plus a change-count summary and a structured JSON change list (type, page, changed fragment, source/target text, table cell, style changes). Optional `sourcePassword` / `targetPassword`.
- **AnalyzeChanges** — return the differences as **structured data only** (same JSON change list as Compare) **without** rendering or saving a result file. Cheaper than Compare; for summarizing/reasoning about what changed. Optional `sourcePassword` / `targetPassword`.
- **GetDocumentInfo** — inspect a single source document and return file type, page count, file size, and per-page dimensions as JSON; no comparison. Optional `password`.

Supported formats: PDF, DOCX, XLSX, PPTX, ODT, ODS, ODP, RTF, TXT, HTML, and 30+ more.

## Building this repo

```bash
dotnet restore
dotnet build src/GroupDocs.Comparison.Mcp.sln -c Release
dotnet test  src/GroupDocs.Comparison.Mcp.sln -c Release
dotnet run --project src/GroupDocs.Comparison.Mcp     # run the server locally (stdio)
pwsh ./build.ps1                                       # local pack to ./build_out
```

## Environment variables

Only these three are supported — do **not** add new ones without updating `server.json`, `docker-compose.yml`, and `README.md` together:

- `GROUPDOCS_MCP_STORAGE_PATH` — base folder for input + output (defaults to cwd)
- `GROUPDOCS_MCP_OUTPUT_PATH` — optional, routes output files to a separate folder
- `GROUPDOCS_LICENSE_PATH` — path to `GroupDocs.Total.lic`; omit for evaluation mode (watermarked output)

## Conventions (see AGENTS.md for the full list)

- **Target framework is `net10.0` only** — required by `dnx` and the MCP SDK.
- **Tools need rich `[Description("...")]` strings** — AI agents read these over MCP; write task-oriented sentences.
- **Versioning is CalVer `YY.MM.N`** and lives in two places that must stay in lockstep: `build/dependencies.props` (`<GroupDocsComparisonMcp>`) and `src/GroupDocs.Comparison.Mcp/.mcp/server.json`. `build.ps1` enforces this at pack time. Do not hardcode the version in the `.csproj`.
- **Do not remove** `<PackageType>McpServer</PackageType>` or `<ToolCommandName>groupdocs-comparison-mcp</ToolCommandName>` from the csproj — NuGet discoverability and `dnx` depend on them.
- **Tests use xUnit + Moq**; any behaviour change adds a `changelog/NNN-slug.md` entry.
- Do not edit `obj/` or `build_out/` (build artifacts).

## What this is NOT

- Not the GroupDocs.Comparison **SDK** itself — that is a separate package (`GroupDocs.Comparison`); this server wraps it.
- Not **GroupDocs.Comparison Cloud** (a separate REST API product).
- Integration tests for this server live in a separate repo: https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp.Tests

## Links

- NuGet: https://www.nuget.org/packages/GroupDocs.Comparison.Mcp
- Repository: https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp
- GroupDocs.Comparison docs: https://docs.groupdocs.com/comparison/net/
- How-to articles (blog): https://blog.groupdocs.com/categories/groupdocs.comparison-product-family/
