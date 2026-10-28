# Gem TD (unity-gem-td)

Solo Steam Windows roguelite TD — build-crafting via universal support gems + map expansion.

## Design docs (not in this repo)

**Always check the docs repo** before assuming something is missing — designs, plans, SDD ledgers, and status live there, not in this Unity project.

Docs checkout: `E:\Projects\Docs\project-docs-unity-gem-td` on branch `docs/unity-gem-td`.

Gem TD tree: `E:\Projects\Docs\project-docs-unity-gem-td\Unity\unity-gem-td`

| Path | Contents |
| --- | --- |
| `HOME.md` | Docs map |
| `PROJECT_CONTEXT.md` | Current build state, known gaps, and next step |
| `GDD.md` / `ARCHITECTURE.md` / `UI-SPEC.md` / `DAMAGE-MODEL.md` | Canonical design |
| `ROADMAP.md` | Current gate and authorized next work |
| `TEST_STRATEGY.md` | Done criteria and verification reporting |
| `wiki/catalog/` | Generated per-tower (and later per-gem) reference |
| `planning/` | Phase designs, implementation plans, STATUS. **Phase 2 is closed** — do not execute `phase2-*` or PR6 plans. |
| `planning/handoff.md` | Demo / Early Access set: 29 towers. Full catalog is out of this version. On `feat/phase3-campaign`, custom grass is the only grass system. Attack VFX prefabs are assigned on the tower and payload assets. Delivery timing and damage stay as accepted. The wave-50 Play Mode playthrough is a pass for now (2026-10-15; the run ended in a loss before wave 50). Bloom + Vignette were retuned the same day. Playable bow ice, fire, and physical shots use AllIn1 wrappers; Fireball is unchanged; Lightning Arrow stays on the generic bolt. Other VFX art and SFX clips are later. |
| `planning/sdd/` | Superpowers / SDD session ledger (briefs, reports, `progress.md`) |

**Do not use Notion.** The July 2026 Gem TD Notion tree is retired and is not a source.

**Do not** put design docs, plans, or `.superpowers` / SDD scratch in this game repo. If a skill defaults to `<game-repo>/.superpowers/sdd`, write to `planning/sdd/` in the docs repo instead.

`ARCHITECTURE.md` is the technical-design owner for this project; do not add a
second `TECHNICAL_DESIGN.md`.

## Before working

1. Read `HOME.md` and `PROJECT_CONTEXT.md`.
2. Read the relevant canonical owner: `GDD.md`, `ARCHITECTURE.md`,
   `UI-SPEC.md`, or `DAMAGE-MODEL.md`.
3. Read `ROADMAP.md`, then `planning/STATUS.md`, `planning/handoff.md`, and
   the relevant design/plan or SDD brief.
4. Inspect `git status` before editing and preserve unrelated changes.

Use SCE only after the docs repo: search `unity-gem-td`, then retrieve the
needed document. Use Context7 for current library/API documentation.

## Agent rules

Follow the always-on and scoped rules in `.cursor/rules/`, especially
`project-conventions.mdc`, `advisor.mdc`, `scene-wiring.mdc`,
`gameplay-logic.mdc`, and `presentation-boundaries.mdc`.

1. **Docs repo first** for product/design/plan/SDD context (absolute path above), then **SCE** → Context7 → web. Report SCE gaps; if docs existed only under docs-repo and were missed, say so. **SCE query:** search `unity-gem-td` (repo / docs-tree name). Do not search “Gem TD” — that string does not retrieve this project.
2. **Pushback** when a suggestion is suboptimal: goal → why not → better option → why → when to revisit.
3. **No gameplay reflection** — explicit factories / SO methods.
4. **Data in ScriptableObjects**; thin MonoBehaviours; plain C# domain.
5. **Object pools** for spawnables; no `System.Linq` in combat ticks.
6. **Protected `main`** — feature branch + PR only; never push straight to main.
7. **No auto commit/push** — never `git commit` or `git push` unless the user explicitly asks.
8. **Conventional TD shell** — gems/sockets are the unique fantasy; place/select/sell/waves/hotkeys/previews follow **Bloons TD 5/6** first unless GDD/UI-SPEC override (no Tower Details Upgrade button; tower types level from draft). See `.cursor/rules/gem-td-bloons-mechanics.mdc`.
9. **Prefab-based UI** — panel shells are prefabs pre-placed in the scene, referenced via serialized `[SerializeField]` fields. No ad-hoc UI hierarchy or `RectTransform` math. **Fixed slots:** pre-place N instances (Draft, Codex). **Variable lists:** **bindable prefab view hierarchy** — pool child-view prefabs under serialized parents; `Bind(domainData)` per tier (`RunSummaryController` → `RunSummarySection` → `RunSummaryElement`). Gameplay spawnables may `Instantiate`/pool. TMP only. No new bootstrap/wire-menu code. **Same-prefab:** inspector-drag refs; no `AddComponent`/`GetComponent` in `Awake`/`Start` for prefab children. See `UI-SPEC.md` "Implementation conventions" and "Bindable prefab view hierarchy".

## Verification

Run verification proportionate to the change unless the user explicitly narrows
the request. Domain changes require focused EditMode tests. Scene, prefab, and
UI changes require serialized inspection and a Unity console check; use the
manual validator when available. Play Mode and rendered behavior must be
reported separately and may only be called verified after they actually run.
Never claim a test or runtime check that was not performed.

## Stack notes

UGUI + feature.2d (UI sprites). LitMotion for juice. Custom PathGraph (no NavMesh). See ARCHITECTURE.md.
