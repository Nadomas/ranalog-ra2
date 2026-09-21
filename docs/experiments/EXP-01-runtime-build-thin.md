# EXP-01 — Runtime Modular Robot (thin / local)

> **Status:** thin PASS (STAGE 1 scope only)  
> **Date:** 2026-08-21  
> **Full EXP-01** (blueprint → data-driven assemble) remains open for STAGE 3.

## What was tested (thin)

Editor builder (`PhysicsTestSceneBuilder`) assembles a minimal robot hierarchy at build time:

- chassis `Rigidbody` + compound wheel colliders
- S1-03: one additional wheel `Rigidbody` + `HingeJoint` connected to chassis
- stable IDs via GameObject names (`Robot_A`, `Wheel_FL`, …)

Not a serialized blueprint runtime yet — proves PhysX accepts the resulting body/joint graph without explosion.

## Metrics (PhysicsTest / S1-03)

| Metric | Value |
|--------|--------|
| Bodies on Robot_A | 2 |
| Joints on Robot_A | 1 |
| First FixedUpdate stability | ok (no NaN) |
| Drive after spawn | ok (S1-02 command → chassis forces) |
| Repeatability | 10/10 Play Mode runs (S1-03 verifier) |

## Verdict (STAGE 1)

Thin EXP-01 **PASS** for demo complexity. Full data-driven construction remains `[EXPERIMENT REQUIRED]` for later stages.
