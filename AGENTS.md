# Gem TD (unity-gem-td)

Solo Steam Windows roguelite TD — build-crafting via universal support gems + map expansion.

## Design docs (not in this repo)

**Always check the docs repo** before assuming something is missing — designs, plans, SDD ledgers, and status live there, not in this Unity project.

Docs checkout: `E:\Projects\Docs\project-docs-unity-gem-td` on branch `docs/unity-gem-td`.

Gem TD tree: `E:\Projects\Docs\project-docs-unity-gem-td\Unity\unity-gem-td`

| Path | Contents |
| --- | --- |
| `HOME.md` | Docs map |
| `PROJECT_CONTEXT.md` | **The single owner of current build state**, known gaps, and next step |
| `GDD.md` / `ARCHITECTURE.md` / `UI-SPEC.md` / `DAMAGE-MODEL.md` | Canonical design |
| `ROADMAP.md` | Current gate and authorized next work |
| `TEST_STRATEGY.md` | Done criteria and verification reporting |
| `wiki/catalog/` | Generated per-tower (and later per-gem) reference |
| `planning/` | Phase designs, implementation plans, STATUS. **Phase 2 is closed** — do not execute `phase2-*` or PR6 plans. |
| `planning/handoff.md` | Release-scope guard: 29-tower Demo / Early Access set, deferred tables, reimport hazards |
| `planning/handoff-slices.md` | Shipped slices and locked contracts (record — not a work list) |
| `planning/handoff-import-recipe.md` | Full-release catalog import procedure (out of this version) |
| `planning/STATUS.md` | Phase checklist. Delivered detail is in `planning/STATUS-delivered-phases.md` |
| `planning/sdd/` | Superpowers / SDD session ledger (briefs, reports, `progress.md`) |

**Do not use Notion.** The July 2026 Gem TD Notion tree is retired and is not a source.

**Do not** put design docs, plans, or `.superpowers` / SDD scratch in this game repo. If a skill defaults to `<game-repo>/.superpowers/sdd`, write to `planning/sdd/` in the docs repo instead.

`ARCHITECTURE.md` is the technical-design owner for this project; do not add a
second `TECHNICAL_DESIGN.md`.

## Before working

Read in tiers. Do not read the whole tree; most of it is ledger.

| Tier | Read | When |
| --- | --- | --- |
| 1 | `HOME.md`, `PROJECT_CONTEXT.md`, `ROADMAP.md` | every session |
| 2 | the one canonical owner for the subject (`GDD` / `ARCHITECTURE` / `UI-SPEC` / `DAMAGE-MODEL`) | every session that touches it |
| 3 | `planning/handoff.md` | only when editing towers, gems, combat, VFX, or release scope |
| 4 | `planning/STATUS.md` | only when phase detail is needed |
| 5 | the newest 2–3 entries of `planning/sdd/progress.md` | only when resuming mid-task. It is newest-first and append-only; never read it whole |
| — | `handoff-slices.md`, `handoff-import-recipe.md`, `STATUS-delivered-phases.md` | archaeology, or a later full-release branch |

Then inspect `git status` before editing and preserve unrelated changes.

**Living docs stay lean.** `HOME.md`, `PROJECT_CONTEXT.md`, `ROADMAP.md`,
`planning/STATUS.md`, and `TEST_STRATEGY.md` stay under ~1,500 words each;
`planning/handoff.md` under ~2,000 because it carries the deferred-catalog
tables. These are tripwires, not targets: if a living doc crosses its number, ask
whether the overflow is **history** (move it to the matching append-only record)
or **scope** (keep it, and the record is what shrinks). Do not shorten live scope
to satisfy a count. When work ships, its narrative moves to
`planning/sdd/progress.md` in the same edit; a delivered phase becomes one table
line plus a pointer.

**Current state has exactly one home: `PROJECT_CONTEXT.md`.**
- Do not restate it in rules, handoff files, or `planning/NOTES.md`.
- Keep its `## Current state` bullets as standing **contracts** ("delivery timing
  is accepted; do not change it"), not narrative ("we tested X on date Y").
  Narrative goes in `planning/sdd/progress.md`.
- **Freshness check before working:** compare its `**Updated:**` date against the
  newest `## ` heading in `planning/sdd/progress.md`. If the progress entry is
  newer, the owner is stale — reconcile it as the first task, before code.

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
7. **No auto commit/push** — never `git commit` or `git push` unless the user explicitly asks to commit or push. Sync, update, finalize, record, save, and "docs first" do not authorize either one. This includes the docs checkout.
8. **Named scope only** — change only the files, fields, and assets the user names. If that change would alter something unnamed through a shared path, keep the unnamed behavior as it is and do not edit its values. Do not restructure it when the named change would not reach it. Card-chrome colors and tag-chip colors are independent. Do not rewrite a serialized asset so that it drops or resets values the user set. Script defaults are not those values.
9. **Conventional TD shell** — gems/sockets are the unique fantasy; place/select/sell/waves/hotkeys/previews follow **Bloons TD 5/6** first unless GDD/UI-SPEC override (no Tower Details Upgrade button; tower types level from draft). See `.cursor/rules/gem-td-bloons-mechanics.mdc`.
10. **Prefab-based UI** — panel shells are prefabs pre-placed in the scene, referenced via serialized `[SerializeField]` fields. No ad-hoc UI hierarchy or `RectTransform` math. **Fixed slots:** pre-place N instances (Draft, Codex). **Variable lists:** **bindable prefab view hierarchy** — pool child-view prefabs under serialized parents; `Bind(domainData)` per tier (`RunSummaryController` → `RunSummarySection` → `RunSummaryElement`). Gameplay spawnables may `Instantiate`/pool. TMP only. No new bootstrap/wire-menu code. **Same-prefab:** inspector-drag refs; no `AddComponent`/`GetComponent` in `Awake`/`Start` for prefab children. See `UI-SPEC.md` "Implementation conventions" and "Bindable prefab view hierarchy".

## Verification

Run verification proportionate to the change unless the user explicitly narrows
the request. Domain changes require focused EditMode tests. Scene, prefab, and
UI changes require serialized inspection and a Unity console check; use the
manual validator when available. Play Mode and rendered behavior must be
reported separately and may only be called verified after they actually run.
Never claim a test or runtime check that was not performed.

## Stack notes

UGUI + feature.2d (UI sprites). LitMotion for juice. Custom PathGraph (no NavMesh). See ARCHITECTURE.md.
