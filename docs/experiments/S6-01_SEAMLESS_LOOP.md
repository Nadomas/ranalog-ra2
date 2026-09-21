# S6-01 — Seamless Design ↔ Configure ↔ Test

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S6-01 Seamless Loop)`

## Goal

Prove Fast Iteration: switch Design ↔ Configure ↔ Test without level load; keep one working blueprint; Test spawn/reset is cheap and reproducible.

## Code

| Piece | Role |
|-------|------|
| `RobotWorkshopSession` | Mode state, retain blueprint, spawn/despawn/reset Test |
| `RobotSeamlessLoopVerifier` | Timed loop + reverse wiring retain + no GO leak |

## Pass log

```
[S6-01] VERIFIER_DONE pass=True mode_fast=True spawn_ok=True retained=True same_config=True no_leak=True ms_design=0.0 ms_cfg=0.0 ms_test=18.9 ms_back=0.2 ms_retest=2.1 ms_reset=2.3 ...
```

## Not in this spike

- Additive multi-scene U-SCN production polish  
- Online Test vs opponents  
- UI chrome
