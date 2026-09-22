# S13-02 — Freehand chassis polygon gizmo

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** S4-02 polygon editor · S11-06 Design chrome  
> **Local-only presentation** — blueprint data only; no dynamic Rigidbody writes

## Goal

In Design mode, chassis baseplate vertices are visible on the floor and can be dragged (or set via API) without leaving the MVP workshop shell.

## Implementation

| Piece | Role |
|-------|------|
| `RobotChassisPolygonGizmo` | LineRenderer outline + sphere handles; mouse drag on XZ plane |
| `RobotWorkshopChrome.TryDesignSetPoint` / `TrySelectPolyIndex` | Absolute edit + selection |
| Smoke | `TrySmokeFreehandGizmo` after Design enter |

## Pass log

```
[S13-02] FREEHAND_GIZMO_SMOKE pass=True set=True moved=True visible=True sel=0
[S11-07] SMOKE_DONE … freehand=True
```

## Out of scope

Add/remove vertex by double-click, mesh extrusion preview, paint shop.
