# S11-E2E — Design→Configure→Test→Fight→Results (one scene)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S11 E2E Loop)`  
> **Date:** 2026-09-21

## Goal

Single PhysicsTest Play that chains workshop Design → polygon nudge → Configure binding groups → Test drive → local Immobilized fight → results persist/view.

## Code

| Piece | Role |
|-------|------|
| `RobotMvpE2EVerifier` | Orchestrates the loop (local combat only — no UDP) |
| Reuses | WorkshopSession, ChassisPolygonEditor, ControlConfigurer groups, Damage/Immobility, MatchResultsStub |

## Acceptance

- Mode switches ≥2
- Polygon nudge OK; points ≤16
- Drive/Turn group bindings applied
- Fight ends Immobilized
- Results JSON + thin view visible

## Not in this spike

- UDP MP path (covered by S11 glue / S9)
- Product HUD polish

## Pass log

```
[S10-01] RESULTS side=e2e session=s11-e2e-075948 finished=True reason=Immobilized winner=1 loser=0 ...
[S10-02] PERSIST ok path=.../match-s11-e2e-075948.json
[S11-E2E] VERIFIER_DONE pass=True poly=True/ pts_ok=True group=True/ fight=True reason=Immobilized winner=1 results=True path=... load_err=none switches=2
```
