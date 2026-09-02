# Backlog & Known Issues

Running list of ideas, planned work, and known limitations for the
GroupDocs.Comparison MCP server. Grouped by topic. Terse on purpose — each line is
a ticket, not an essay. `[ ]` = open, `[x]` = shipped (kept for context).

**Current surface (26.9.0):** `compare`, `analyze_changes`, `get_document_info`.

---

## Confirmed defects — external audit, 2026-08-16

Source: black-box test round against `ghcr.io/groupdocs-comparison/comparison-net-mcp:latest`
(26.7.5, licensed), 46 family-wide defects reported and all 46 independently reproduced with
control calls. A later validation round found **zero false positives**.

`S#` = shared core (`GroupDocs.Mcp.Core`) · `M#` = this repo · `P#` = GroupDocs.Comparison library

**Verdict: the cleanest product of the twelve.** No product-library defects found; the honesty
test passed (byte-identical inputs → `No changes detected`, no invented diffs).

### Shared core — fixed once in `GroupDocs.Mcp.Core`, lands here on the next bump

- [ ] **S1** Passing `fileName` crashes any tool — **High**. Unhandled `ArgumentException` in
      `FileResolver.ResolveAsync`; client sees only `An error occurred invoking '<tool>'`. The
      tool descriptions *recommend* this form and the schema makes it legal.
      *Proof:* `get_document_info {"file":{"fileName":"03_pages_text.pdf"}}` → opaque error;
      `filePath` control succeeds.
- [ ] **S2** Missing files return an opaque error — **High**. The `Available files:` listing the
      descriptions promise is built and then thrown away in stderr. Also silently capped at 20
      entries with no truncation marker.
- [ ] **S3** `isError` is set on crashes but not on real failures — **Med**. The flag means "we
      crashed", not "the operation failed", so a client cannot detect failure programmatically.

Nothing to do in this repo for S1–S3 beyond re-testing after the Core bump.

### MCP wrapper — this repo

- [ ] **M3** `analyze_changes` description points at a tool that does not exist — **Low**.
      It says *"cheaper than `compare_documents`"* and *"use `compare_documents` instead"*; the
      real tool is `compare`. An agent following the advice calls a nonexistent tool.
      *Fix:* two string replacements. **P1** — 2-minute fix, prevents a guaranteed agent failure.

### Product library — upstream

None found.

---

## Known issues & limitations

- **`compare` / `analyze_changes` cannot run on Linux or macOS** — the render path uses
  `System.Drawing.Common`, Windows-only since .NET 7. Reproduced in a container: `libgdiplus`
  loads, but the `gdiplus.dll` P/Invoke is declared in `System.Private.Windows.Core`, and
  resolving it correctly only surfaces `PlatformNotSupportedException`. Neither `Aspose.Drawing`
  nor retargeting to `net8.0` helps. **No MCP-layer fix exists** — needs the engine to migrate off
  System.Drawing as Metadata already has. Comparing *identical* documents and `get_document_info`
  are unaffected.
- A `GdiPlusResolver` shipped in 26.7.1 to work around the above **made it worse** (registered on
  the wrong assembly *and* collided with System.Drawing's own resolver) and was reverted in
  26.7.2. **Do not retry that approach.**
- `System.Drawing.EnableUnixSupport` is inert — the flag was removed in System.Drawing.Common 7.0+.
  Kept in the csproj as a marker only.

---

## Tools & functionality

- [ ] **M3** description fix (see above). **P1**
- [ ] `compare` — accept an array of targets for multi-document comparison; today it is source +
      one target. **P2**
- [ ] Expose an output-name parameter so callers control the result path instead of relying on the
      `<source-stem>_compared<ext>` convention. **P2**

## Testing & CI

- [ ] Add the two mandatory probes every product suite is missing: the **`fileName`-only form**,
      and a **missing file** asserting the promised `Available files:` text. Today's oracle
      (`IsError || contains("not found")`) passes on the exact defect reported. **P1**
- [ ] Add a `channel: [dnx, docker]` axis — the current matrix is dnx-only, which is why the
      family's Linux packaging defects are invisible to CI. **P1**
- [ ] Per-tool Linux smoke test in image CI: call every tool once in the built container. **P1**
- [ ] Not covered by any test today: password-protected comparison, base64 (`fileContent`) input,
      ODT/HTML/RTF inputs. **P2**
- [ ] Linux integration leg is red for the upstream reason above — split it out or mark it
      expected-fail so it stops masking real regressions. **P2**

## Documentation & discoverability

- [ ] Document the Linux/macOS limitation prominently in README and in the `compare` tool
      description — today the channels read as interchangeable. **P1**
- [ ] Licensing section covering the metered option once it ships. **P1**
- [ ] Refresh the MCP Registry description when the tool set changes.

## Platform & infra (longer-term)

- [ ] Metered licensing (`GROUPDOCS_METERED_PUBLIC_KEY` / `_PRIVATE_KEY`) via
      `GroupDocs.Mcp.Core`, plus the `get_license_status` tool. **P1**
- [ ] HTTP/SSE transport for shared/team deploys (stdio stays default). **P2**
- [ ] Remote storage (URL / S3) via `GroupDocs.Mcp.Core`. **P2**

---

*Evidence: `TEMP_ThirdPartyAnalysis/comparison.md` (per-product findings),
`ALL-PRODUCTS-REPORT.md` (10-product sweep), `VALIDATION-REPORT.md` (why the green suites miss
these). Conventions: any behaviour change ships with a `changelog/NNN-*.md` entry and a CalVer
bump. Integration tests target the published NuGet via `dnx`, so new-tool tests only pass once the
matching version is live.*
