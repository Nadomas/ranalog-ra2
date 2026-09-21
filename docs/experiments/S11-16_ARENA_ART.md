# S11-16 — Stronger thin arena art (MVP playable)

> **Status:** **PASS** (2026-09-21 smoke)  
> **Depends on:** S11-11 feel polish  
> **Presentation-only** — no physics authority changes

## Goal

Make the pit read as an arena (fence, posts, stands, lane marks, key/fill lights) without shipping a content catalog or imported meshes.

## Code

| Piece | Role |
|-------|------|
| `RobotMvpArenaDressing` | Procedural decoration under `ArenaDressing` |
| `RobotMvpPlayableApp.EnsureArenaDressing` | Calls builder from `EnsureWorld` |
| Physics `ArenaBounds` | Unchanged invisible colliders |

## Pass log

```
[S11-16] ARENA_DRESSING ok kids=…
[S11-16] ARENA_SMOKE pass=True
[S11-07] SMOKE_DONE … arena=True
```

## Out of scope

Imported arena meshes, crowd textures, hazard gameplay props, Stage 8 catalog art.
