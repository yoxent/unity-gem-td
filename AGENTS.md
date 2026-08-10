# Gem TD (unity-gem-td)

Solo Steam Windows roguelite TD — build-crafting via universal support gems + map expansion.

## Design docs (not in this repo)

**Always check the docs repo** before assuming something is missing — designs, plans, SDD ledgers, and status live there, not in this Unity project.

`E:\Projects\Docs\project-docs\Unity\unity-gem-td`

| Path | Contents |
| --- | --- |
| `GDD.md` / `ARCHITECTURE.md` / `UI-SPEC.md` | Canonical design |
| `planning/` | Phase designs, implementation plans, STATUS |
| `planning/sdd/` | Superpowers / SDD session ledger (briefs, reports, `progress.md`) |

**Do not** put design docs, plans, or `.superpowers` / SDD scratch in this game repo. If a skill defaults to `<game-repo>/.superpowers/sdd`, write to `planning/sdd/` in the docs repo instead.

## Agent rules

1. **Docs repo first** for product/design/plan/SDD context (absolute path above), then **SCE** → Context7 → web. Report SCE gaps; if docs existed only under docs-repo and were missed, say so.
2. **Pushback** when a suggestion is suboptimal: goal → why not → better option → why → when to revisit.
3. **No gameplay reflection** — explicit factories / SO methods.
4. **Data in ScriptableObjects**; thin MonoBehaviours; plain C# domain.
5. **Object pools** for spawnables; no `System.Linq` in combat ticks.
6. **Protected `main`** — feature branch + PR only; never push straight to main.
7. **No auto commit/push** — never `git commit` or `git push` unless the user explicitly asks.
8. **Conventional TD shell** — gems/sockets are the unique fantasy; place/select/sell/upgrade/waves/hotkeys/previews follow **Bloons TD 5/6** first (see `.cursor/rules/gem-td-bloons-mechanics.mdc`).
9. **Prefab-based UI** — UI panels are prefabs pre-placed in the scene, referenced via serialized `[SerializeField]` fields (inspector-dragged). Runtime NEVER calls `new GameObject`/`Instantiate` or sets `RectTransform` math for **static, bounded UI** — use layout groups in the inspector. **Exception:** runtime-generated/unbounded entities (enemies, projectiles, gems, VFX) may be instantiated/pooled at runtime. All text is **TextMeshPro** (`TMP_Text`), not legacy `Text`. No new bootstrap/wire-menu code (existing `Phase2Pr*WireScene` frozen, deleted pre-release). See `UI-SPEC.md` "Implementation conventions."

## Stack notes

UGUI + feature.2d (UI sprites). LitMotion for juice. Custom PathGraph (no NavMesh). See ARCHITECTURE.md.
