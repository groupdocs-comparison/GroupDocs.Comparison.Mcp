# AGENTS.md — Guide for AI coding agents

Brief orientation for AI coding agents (Claude Code, Copilot, Cursor, Aider, Amp, Codex) working in this repository.

## What this repo is

A standalone **MCP server** for [GroupDocs.Comparison for .NET](https://products.groupdocs.com/comparison) — exposes document comparison operations as AI-callable tools via the Model Context Protocol.

Published to NuGet as `GroupDocs.Comparison.Mcp` with the `McpServer` package type, and to `ghcr.io/groupdocs-comparison/comparison-net-mcp` + `docker.io/groupdocs/comparison-net-mcp` as a container image.

## MCP tools exposed

| Tool | Description |
|---|---|
| `Compare` | Compare two documents (source vs target), produce a marked-up result file with the differences highlighted, and return a change-count summary. Supports PDF, Word, Excel, PowerPoint, ODT, RTF, TXT, HTML, and 30+ more formats; takes optional `sourcePassword` / `targetPassword`. |
| `GetDocumentInfo` | Inspect a single source document and return file type, page count, file size, and per-page dimensions as JSON. No comparison performed. Optional `password` for protected documents. |

`Compare` accepts `sourceFile` + `targetFile` as `FileInput` (resolved via `IFileResolver`), plus optional `sourcePassword` / `targetPassword`. `GetDocumentInfo` takes just `file` (`FileInput`) and an optional `password`.

## Folder layout

```
src/                                           ← all projects + sln + Directory.Build.props
  GroupDocs.Comparison.Mcp/
    Program.cs                                 ← host bootstrap + stdio transport
    ComparisonLicenseManager.cs                ← applies GroupDocs.Total license
    Tools/
      CompareTool.cs                           ← [McpServerTool] — Compare
      GetDocumentInfoTool.cs                   ← [McpServerTool] — GetDocumentInfo
    .mcp/
      server.json                              ← NuGet.org reads this to generate mcp.json snippet
    GroupDocs.Comparison.Mcp.csproj            ← PackageType=McpServer + ToolCommandName
  GroupDocs.Comparison.Mcp.Tests/
  GroupDocs.Comparison.Mcp.sln
  Directory.Build.props
build/
  dependencies.props                           ← single source of truth for all versions
changelog/                                     ← one MD file per change (see changelog/README.md)
docker/
  Dockerfile                                   ← multi-stage, runtime on aspnet:10.0
  docker-compose.yml
.github/workflows/                             ← build_packages.yml, run_tests.yml, publish_prod.yml, publish_docker.yml
```

## Dependencies

- `GroupDocs.Mcp.Core` + `GroupDocs.Mcp.Local.Storage` — infrastructure NuGet packages from the [GroupDocs.Mcp.Core](https://github.com/groupdocs/GroupDocs.Mcp.Core) repo
- `GroupDocs.Comparison` — the actual comparison engine
- `ModelContextProtocol` — MCP SDK for .NET
- `Microsoft.Extensions.Hosting` — host builder for the stdio server

## Commands you can run

```bash
# Restore + build
dotnet restore
dotnet build src/GroupDocs.Comparison.Mcp.sln -c Release

# Run tests
dotnet test src/GroupDocs.Comparison.Mcp.sln -c Release

# Run the server locally (stdio)
dotnet run --project src/GroupDocs.Comparison.Mcp

# Local pack (writes to ./build_out) — validates server.json version matches dependencies.props
pwsh ./build.ps1

# Build + run the Docker image
docker build -f docker/Dockerfile -t comparison-net-mcp:local .
docker run --rm -i -v $(pwd)/documents:/data comparison-net-mcp:local
```

## Version scheme

CalVer `YY.MM.N`. The version lives in **two** places that MUST stay in lockstep:
1. `build/dependencies.props` → `<GroupDocsComparisonMcp>`
2. `src/GroupDocs.Comparison.Mcp/.mcp/server.json` → both top-level `"version"` and `packages[0].version`

`build.ps1` enforces this at pack time (`Assert-ServerJsonVersionMatchesDependencies`) — if they drift, the build fails.

## House rules

1. **Tools must have rich `[Description("...")]` strings** — these are what AI agents read via the MCP protocol. Write them as task-oriented sentences, not method-signature summaries.
2. **Never add new env vars beyond** `GROUPDOCS_MCP_STORAGE_PATH`, `GROUPDOCS_MCP_OUTPUT_PATH`, `GROUPDOCS_LICENSE_PATH` without updating `server.json`, `docker-compose.yml`, and `README.md` together.
3. **Tests use xUnit + Moq** — mock `IFileResolver`, `IFileStorage`, `ILicenseManager`, `OutputHelper`.
4. **Changelog entries required** — any PR that changes behaviour adds `changelog/NNN-slug.md`.
5. **Do not edit `obj/` or `build_out/`** — build artifacts.
6. **Target framework is `net10.0` only** — required by `dnx` and the MCP SDK.

## Release flow

See [RELEASE.md](RELEASE.md) for the exact per-release checklist.

## What NOT to change

- Do not hardcode the version in `.csproj` — it flows from `$(GroupDocsComparisonMcp)` in `dependencies.props`.
- Do not remove the `<PackageType>McpServer</PackageType>` or `<ToolCommandName>groupdocs-comparison-mcp</ToolCommandName>` from the csproj — NuGet.org discoverability and `dnx` invocation depend on them.
- Do not change the `.mcp/server.json` schema URL without cross-checking with the NuGet MCP docs.
