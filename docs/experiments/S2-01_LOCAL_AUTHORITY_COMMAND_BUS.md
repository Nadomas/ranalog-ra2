# S2-01 — Local Authority Command Bus

> **Status:** PASS (1 Play Mode auto verifier run)  
> **Date:** 2026-08-21  
> **Plan:** [S2-00_STAGE2_SPIKE_PLAN.md](./S2-00_STAGE2_SPIKE_PLAN.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-01 Dual Authority)`

## Hypothesis

Two PhysicsTest robots can be driven **only** through a host-like command bus (`PhysicsTestDriveCommand` → `PhysicsTestLocalAuthority` → `PhysicsTestDrive`) without a net package, preserving the S1-02 force model and S1-03 hinge on Robot_A.

## Setup

| Item | Value |
|------|--------|
| Robot_A | Drive + hinged Wheel_FL + CommandSource (WASD/Space), sourceId 0 |
| Robot_B | Drive + CommandSource (Arrows/RightShift), sourceId 1 |
| Authority | `LocalAuthority` applies pending commands in FixedUpdate (order -100) |
| Packages | **None added** |

## Verifier result

Protocol: A forward → B forward → dual drive → unauthorized Submit → direct cheat `SetCommand` then authority idle overwrite.

```
[S2-01] VERIFIER_DONE nan=False A_delta=10.714 B_delta=12.270 both_moved=True
reject_ok=True cheat_overwritten=True rejected_total=1 applied=372
```

## Verdict

**S2-01 PASS** — local authority command path is ready for S2-02 (provisional transport). Networking solution remains **Open** / `[EXPERIMENT REQUIRED]`.

## Manual play

- A: WASD + Space  
- B: Arrows + Right Shift  
