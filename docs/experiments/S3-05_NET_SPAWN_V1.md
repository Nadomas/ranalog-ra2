# S3-05 — Host Net Spawn of RA2 v1 Blueprint (loopback)

> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S3-05 Net Spawn V1)`

## Hypothesis

Host admit of `ra2.robot_blueprint.v1` uses the same spawn path as S3-03. Invalid v1 (wiring without Control Board) fails closed.

## In scope

- Valid tank-steer v1 spawn + drive via loopback commands
- Reject `CreateInvalidNoControlBoard`
- Client graph matches v1 component ids

## Out of scope

- Cross-process UDP
- Per-motor torque from wiring

## Pass log

```
[S3-05] VERIFIER_DONE pass=True nan=False moved=True spawn_ok=True reject_ok=True schema_ok=True board_ok=True assemble_ok=True delta=13.724 live=1 accepted=1 rejected=1 reject_reason=control_board_required schema=ra2.robot_blueprint.v1 spawned_from=v1_loopback
```

**Date:** 2026-08-24 · Play 1× PASS
