# Codex CLI (OpenAI)

```bash
codex mcp add groupdocs-comparison -- dnx GroupDocs.Comparison.Mcp --yes
```

Or add to `~/.codex/config.toml`:

```toml
[mcp_servers.groupdocs-comparison]
command = "dnx"
args = ["GroupDocs.Comparison.Mcp", "--yes"]

[mcp_servers.groupdocs-comparison.env]
GROUPDOCS_MCP_STORAGE_PATH = "/path/to/documents"
# GROUPDOCS_LICENSE_PATH = "/path/to/GroupDocs.Total.lic"   # omit for evaluation mode
```

Pin a version by replacing `GroupDocs.Comparison.Mcp` with `GroupDocs.Comparison.Mcp@26.7.3`.
