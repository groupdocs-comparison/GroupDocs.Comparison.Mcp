# Claude Code

```bash
claude mcp add groupdocs-comparison -- dnx GroupDocs.Comparison.Mcp --yes
```

With storage folder and license:

```bash
claude mcp add groupdocs-comparison -e GROUPDOCS_MCP_STORAGE_PATH=/path/to/documents -e GROUPDOCS_LICENSE_PATH=/path/to/GroupDocs.Total.lic -- dnx GroupDocs.Comparison.Mcp --yes
```

Pin a version by replacing `GroupDocs.Comparison.Mcp` with `GroupDocs.Comparison.Mcp@26.7.5`.
