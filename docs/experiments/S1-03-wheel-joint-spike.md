# S1-03 — Wheel / Joint Spike Results

> **Status:** PASS (10/10 Play Mode runs)  
> **Date:** 2026-08-21  
> **Scene:** `Assets/Scenes/PhysicsTest.unity` (builder: S1-03 Wheel Joint)  
> **Plan:** [S1-03_WHEEL_JOINT_SPIKE_PLAN.md](./S1-03_WHEEL_JOINT_SPIKE_PLAN.md)

## Setup

| Item | Value |
|------|--------|
| Joint | `HingeJoint` on `Robot_A/Wheel_FL` |
| Wheel RB mass | 1.2 (~10% of chassis 12) |
| Axis | local `Vector3.up` (cylinder axle after builder `Euler(0,0,90)`) |
| Motor / spring / limits | off |
| `enableCollision` | false (default; no wheel↔chassis collision) |
| Chassis | `FreezeRotationX\|Z` kept (per plan) |
| Drive | unchanged S1-02 forces on chassis |
| Other wheels A / all B | compound colliders only |

## Counts (EXP-02 baseline)

| Robot | Rigidbodies | Joints |
|-------|-------------|--------|
| Robot_A | 2 (chassis + Wheel_FL) | 1 (`HingeJoint`) |
| Robot_B | 1 | 0 |

## Verification (automated `PhysicsTestJointVerifier`)

Protocol per run: drive → reverse → turn L/R → wall approach → push Robot_B.

| Metric | Result |
|--------|--------|
| Runs | **10/10 PASS** |
| NaN / Inf | none |
| Explosion | none (`A_speed` peak ~25, no runaway) |
| Hinge alive / attached | true all runs |
| Floor | ok (y ~0.7) |
| Push Robot_B | yes (`B_delta` ~10) |
| Console errors | none during runs |

Friction: `PhysicsTestSlide` still very slippery (user: “too much” but OK for now). Did **not** retune for this spike.

## Recommendation

- Keep **HingeJoint** as first joint primitive for STAGE 1 → STAGE 2 path.
- Do **not** expand to 4-wheel / suspension yet.
- Open follow-ups (not STAGE 1 blockers): remove `FreezeRotation`, wheel motor torque, friction retune, ConfigurableJoint if modular DOF needs it.

## Verdict

**S1-03 PASS** — minimal hinge wheel attachment is stable enough to treat jointed modular bodies as viable for the multiplayer physics spike (STAGE 2), with upright freeze still a known hack.
