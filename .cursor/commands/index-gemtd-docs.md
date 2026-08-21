Re-index Gem TD design docs into Context Mode for this project only.

## Do this now

1. Call MCP tool **`ctx_index`** on server **Context Mode** (`plugin-context-mode-context-mode`) with exactly:

| Arg | Value |
| --- | --- |
| `path` | `E:\Projects\Docs\project-docs\Unity\unity-gem-td` |
| `source` | `GemTD-docs` |
| `extensions` | `[".md", ".txt"]` |
| `exclude` | `["**/sources/**"]` |
| `maxDepth` | `6` |
| `maxFiles` | `100` |

2. Do **not** index the Unity game repo, `Assets/`, or `sources/` under the docs tree.
3. After success, confirm with a short `ctx_search`:
   - `source`: `GemTD-docs`
   - `queries`: `["Phase 2 PR map", "planning/sdd progress"]`
4. Reply with: files/sections indexed count, and whether the smoke search hit `planning/` docs.

## Notes

- Docs live outside this repo; always use the absolute path above.
- Re-run anytime docs change; same `source` label refreshes the index.
