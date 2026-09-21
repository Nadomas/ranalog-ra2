# S4-02 — Chassis Polygon Editor (thin)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S4-02 Chassis Polygon Editor)`  
> **Date:** 2026-09-21

## Goal

Touchable thin chassis polygon mutator: edit `BaseplatePoints` with ≤16 / ≥3 budgets, IMGUI nudge smoke, then admit+spawn still OK.

## Code

| Piece | Role |
|-------|------|
| `RobotChassisPolygonEditor` | Plain service: set/add/remove/nudge points; reject illegal counts |
| `RobotChassisPolygonChrome` | Local-only IMGUI (prev/next/nudge/add/remove) |
| `RobotChassisPolygonVerifier` | Programmatic edit → reject 17 → restore → validate → spawn |

## Acceptance

- Nudge changes a point
- Fill to 16 then reject 17th (`chassis_points:…>16`)
- Reject &lt;3
- Restored 4-pt polygon admits + spawns
- No new packages; no Transform teleport on dynamic bodies

## Pass log

```
[S4-02] VERIFIER_DONE pass=True before=4 nudged=True/ at_max=True reject17=True/chassis_points:17>16 restore=True/ valid=True admit=True/ok spawn=True reject2=True/chassis_points:2<3 status=nudged i=0 pts=4
```
