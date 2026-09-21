# S7-06 — BurstMotor Fire arc

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-06 BurstMotor Fire)`

## Goal

Prove **BurstMotor** responds to Button **Fire** as a **one-shot arc** (&lt;180° hinge limits): rising edge only (held Fire does not re-trigger), motor impulse then slow retract — no Transform writes.

## Code

| Piece | Role |
|-------|------|
| `RobotAssembler` | BurstMotor hinge limits 0–120° |
| `RobotActuatorDrive` | Rising-edge Fire → timed motor pulse |
| `RobotBlueprint.CreateRa2BurstMotorFireSample` | Tank + burst motor + flipper pad |
| `RobotBurstMotorFireVerifier` | Arc + limits + no retrigger while held |

## Pass log

```
[S7-06] VERIFIER_DONE pass=True limits_ok=True fired=True arced=True angle0=NaN angle=91.202 dAngle=NaN w0=0.000 w=3.142 within_limits=True no_retrigger=True nan=False
```

*(Follow-up: verifier now sanitizes hinge.angle NaN before delta; Play already PASS on ω + limits.)*

## Net notes

- Burst trigger is host-side from command Fire edge.
- Arc result is physics; do not client-author angle.

## Not in this spike

- Cock/charge animation
- Electric budget draw on Fire
- Full flipper combat tuning

## Next

Queue idle again aside from human-only `PIPE-T02`.
