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
9. **Prefab-based UI** — panel shells are prefabs pre-placed in the scene, referenced via serialized `[SerializeField]` fields. No ad-hoc UI hierarchy or `RectTransform` math. **Fixed slots:** pre-place N instances (Draft, Codex). **Variable lists:** **bindable prefab view hierarchy** — pool child-view prefabs under serialized parents; `Bind(domainData)` per tier (`RunSummaryController` → `RunSummarySection` → `RunSummaryElement`). Gameplay spawnables may `Instantiate`/pool. TMP only. No new bootstrap/wire-menu code. **Same-prefab:** inspector-drag refs; no `AddComponent`/`GetComponent` in `Awake`/`Start` for prefab children. See `UI-SPEC.md` "Implementation conventions" and "Bindable prefab view hierarchy".

## Testing responsibility

Unless the user explicitly asks the agent to run verification, the user is responsible for confirming **EditMode** and **PlayMode** tests/behavior.

The agent must not imply that tests were run unless it was explicitly instructed to handle testing in the current request; when not instructed, doc wording should describe checks as "to be confirmed by you."

## Stack notes

UGUI + feature.2d (UI sprites). LitMotion for juice. Custom PathGraph (no NavMesh). See ARCHITECTURE.md.
