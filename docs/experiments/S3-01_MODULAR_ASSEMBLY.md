# S3-01 — Data-Driven Modular Assembly (thin)

> **Status:** PASS (3 Play Mode auto verifier runs)  
> **Date:** 2026-08-21  
> **Plan:** [S3-00_STAGE3_SPIKE_PLAN.md](./S3-00_STAGE3_SPIKE_PLAN.md)  
> **Depends on:** [STAGE2_GO_NOGO.md](./STAGE2_GO_NOGO.md) Provisional GO  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S3-01 Modular Assembly)`

## Hypothesis

A PhysicsTest-like 1v1 pair can be **assembled from blueprint data** (`RobotBlueprint` + `RobotAssembler`) and still drive via `PhysicsTestDrive` forces without NaN — same physical surface Stage 2 authority can later own.

## Method

| Piece | Role |
|-------|------|
| `Ra2.Robot.RobotBlueprint` | Sample A (hinged FL) / B (all hierarchy wheels) |
| `Ra2.Robot.RobotAssembler` | Plain service: parts, RB, HingeJoint from defs |
| `RobotAssemblyVerifier` | Direct `SetCommand` drive smoke (no net in this slice) |

## Metrics (3 runs)

```
[S3-01] VERIFIER_DONE pass=True nan=False both_moved=True hinge_ok=True
A_delta≈2.225 B_delta≈14.488 assembled_from=blueprint
```

- A moves less than B (hinged wheel mass) — expected; both displace.  
- Console: no project errors.

## Packages

**None added.** Serialization format still Open (U-SER).

## Verdict

**S3-01 PASS** — modular data→physics assembly works for the PhysicsTest sample. **S3-02 PASS** (serialize round-trip). **S3-03 PASS** (loopback net spawn). Exit: [STAGE3_EXIT.md](./STAGE3_EXIT.md).

## Open questions

- Schema versioning / JSON vs ScriptableObject (U-SER).  
- Shared spawn path for local test + net host (S3-03).  
- How many bodies stay on the Stage 2 sync surface (ARCH C2/C3).
