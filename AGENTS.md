# Gem TD (unity-gem-td)

Solo Steam Windows roguelite TD — build-crafting via universal support gems + map expansion.

## Design docs (not in this repo)

`E:\Projects\Docs\project-docs\Unity\unity-gem-td`

- `GDD.md` — design
- `ARCHITECTURE.md` — tech
- `UI-SPEC.md` — UI

## Agent rules

1. **SCE first** for docs/APIs → Context7 → web. Report SCE gaps.
2. **Pushback** when a suggestion is suboptimal: goal → why not → better option → why → when to revisit.
3. **No gameplay reflection** — explicit factories / SO methods.
4. **Data in ScriptableObjects**; thin MonoBehaviours; plain C# domain.
5. **Object pools** for spawnables; no `System.Linq` in combat ticks.
6. **Protected `main`** — feature branch + PR only; never push straight to main.

## Stack notes

UGUI + feature.2d (UI sprites). LitMotion for juice. Custom PathGraph (no NavMesh). See ARCHITECTURE.md.
