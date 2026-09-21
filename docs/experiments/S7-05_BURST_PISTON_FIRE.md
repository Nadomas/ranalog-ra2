# S7-05 — BurstPiston Fire (air)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-05 BurstPiston Fire)`

## Goal

Prove **BurstPiston** fires on Button **Fire** rising edge: consumes **AirTank** / `Power.AirTotal` budget, applies relative impulse along a **ConfigurableJoint** slider, soft retract via force (no Transform teleport).

## Code

| Piece | Role |
|-------|------|
| `RobotJointKind.Slider` | Linear actuator connection |
| `RobotAssembler` | ConfigurableJoint limits along axis |
| `RobotActuatorDrive` | Edge Fire + air cost (80) + deny when empty |
| `RobotBlueprintValidator` | BurstPiston requires AirTank + AirTotal |
| `RobotBlueprint.CreateRa2BurstPistonFireSample` | Tank + airtank + piston |
| `RobotBurstPistonFireVerifier` | Fire move+spend; empty → deny |

## Pass log

```
[S7-05] VERIFIER_DONE pass=True fired=True air_start=800.000 air_after=720.000 air_now=0.000 air_spent=True moved=True ext=0.450 speed=0.577 denied=True denies=2 nan=False
```

## Net notes

- Air remaining is host/local sim state; do not trust client air claims.
- Fire rising edge evaluated from authoritative command stream.

## Not in this spike

- ServoPiston continuous Extend/Retract
- Air recharge / rate limiting beyond one-shot cost
- Catalog piston defs

## Next

S7-06 BurstMotor Fire arc (optional same pass).
