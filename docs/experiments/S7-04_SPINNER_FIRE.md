# S7-04 — Spinner / SpinMotor Fire (CW continuous)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-04 Spinner Fire)`

## Goal

Prove Button/Switch digital wiring can drive a **SpinMotor** weapon spinner continuously on **CW** (or Fire-like) channel via hinge motors — no Transform writes on dynamics.

## Code

| Piece | Role |
|-------|------|
| `PhysicsTestDriveCommand.Fire` | Digital intent [0,1]; UDP codec writes/reads (legacy 18-byte packets → Fire=0) |
| `RobotWiringDriveResolver` | Button/Switch slots → effort; Burst* skipped (edge path) |
| `RobotMotorDrive` | Continuous hinge effort for SpinMotor + wheels |
| `RobotBlueprint.CreateRa2SpinnerFireSample` | Tank + spinner hub hinge + blade |
| `RobotSpinnerFireVerifier` | Idle → Fire held spin → release coast |

## Pass log

```
[S7-04] VERIFIER_DONE pass=True hinge_ok=True motor_bound=True motors=5 powered=True spun=True idle_w=0.026 spin_w=12.566 coast_w=0.000 coasted=True nan=False
```

## Net notes

- Fire is part of host command envelope (authority still owns sim).
- Clients must not apply local spinner torque outside host command path.

## Not in this spike

- Flipper / BurstMotor arc (see S7-06)
- Configure UI chrome for Fire slots
- Contact damage from spinning blade (S7-03 already covers contact hits)

## Next

S7-05 BurstPiston Fire (air).
