# S16-01 — Practice Test Room obstacles

> **Status:** **PASS** (2026-09-22)  
> **Reference:** RA2 `Scripts/Practice.py` obstacle packs  
> **Local-only** — cleared on fight start

## Goal

In Test Room, cycle None → Barrels → Blocks → Crates → Cones as dynamic PhysX props (same floor as combat).

## Implementation

| Piece | Role |
|-------|------|
| `RobotMvpPracticeObstacles` | Procedural spawn/clear under `PracticeObstacles` |
| UXML `btn-obstacle-cycle` | Test panel control |
| Fight start | `Clear()` so props do not enter battles |

## Pass log

```
[S16-01] PRACTICE_OBSTACLE_SMOKE pass=True k1=Barrels k2=Blocks kids=3
[S11-07] SMOKE_DONE … obstacle=True
```

## Out of scope

Imported RA2 `.gmf` obstacle meshes, ramps pack, sound hits.
