# STAGE 1 Exit — Local Physics Prototype

> **Date:** 2026-08-21  
> **Verdict:** **EXIT YES** — local physics is a sufficient base for STAGE 2 (multiplayer physics spike).

## Checklist vs TECHNICAL_ROADMAP STAGE 1

| Item | Status |
|------|--------|
| Playable local physics demo (`PhysicsTest`) | Done |
| Drive + collision readable | S1-01, S1-02 PASS |
| Basic joint without sim explosion | S1-03 PASS (10/10) |
| EXP-01 thin report | `EXP-01-runtime-build-thin.md` |
| EXP-02 provisional budget | `EXP-02-bodies-joints-budget.md` |
| EXP-03 local two-robot | `EXP-03-two-robot-local.md` |
| No netcode / no new packages | Honored |

## Known gaps (acceptable for exit; not blockers)

- `FreezeRotationX|Z` upright hack still on
- Only **one** hinged wheel; other three compound
- Slide friction “too much” (accepted for now)
- EXP-02 stress ceiling not measured (provisional yellow only)
- Full blueprint runtime construction deferred

## Recommended next

**STAGE 2** — multiplayer physics prototype on this same physical model (do not gold-plate wheels/friction first).
