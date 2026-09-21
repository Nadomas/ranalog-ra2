# S11-06 — Workshop unified Design/Configure chrome

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S11-06 Workshop Unified Chrome)`

## Goal

One touchable workshop panel: **Design** nudges chassis polygon, **Configure** cycles Drive/Turn bindings, then Test → Prepare Admit → Test Admit Clone — without switching to separate polygon/binding scenes.

## Code

| Piece | Role |
|-------|------|
| `RobotWorkshopChrome` | Design Nudge+X; Configure Drive/Turn/Cycle |
| `RobotWorkshopUnifiedChromeVerifier` | Mode gates + nudge + cycle + admit path |

## Pass log

```
[S11-06] VERIFIER_DONE pass=True leave_design=True reject_nudge=True design=True nudged=True/ reject_cycle=True cfg=True cycled=True/Left/Right/ test=True inst=True admit=True/ admit_test=True/ status=admit_test ms=2,4
```

## Authority / local-only

- Chrome remains local-only; blueprint stays MP-admit compatible.

## Not in this spike

- Full composite binding matrix UI  
- Freehand polygon gizmo
