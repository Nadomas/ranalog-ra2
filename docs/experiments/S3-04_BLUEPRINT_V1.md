# S3-04 — Blueprint V1 RA2-Aligned (chassis + wiring + power)

> **Status:** **PASS** (2026-08-24 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S3-04 Blueprint V1 RA2)`

## Goal

Extend blueprint data beyond S3-02 `v0` placement-only graph toward RA2 workshop semantics:

- chassis polygon + armor + weight class
- component `base` taxonomy (ControlBoard, Battery, SpinMotor, Wheel, …)
- dual power budgets (`ElectricTotal`, air reserved)
- controller slots (Switch / Button / Analog)
- wiring graph (control → component → channel)
- validation fail-closed for spawn admit

## Schema

| Schema | Role |
|--------|------|
| `ra2.robot_blueprint.v0` | S3-02 legacy (components + connections only) |
| `ra2.robot_blueprint.v1` | RA2-aligned fields on same `RobotBlueprint` type |

## Code

| Piece | Role |
|-------|------|
| `RobotBlueprint.CreateRa2TankSteerSample()` | Sample with Control Board, battery, 4 wheels, tank wiring |
| `RobotBlueprintValidator` | IDs, root, chassis ≤16 pts, wiring refs, Control Board if v1 |
| `RobotBlueprintSerializer` | v0 + v1 read; v1 write for v1 samples |
| `RobotWiringDriveResolver` | Thin map Forward-Back / Left-Right → `PhysicsTestDriveCommand` |
| `RobotBlueprintV1Verifier` | JSON round-trip + validate + drive smoke |

## Not in this spike

- Locomotion torque is already driven via wiring (see `RobotMotorDrive`); this spike focuses on RA2-aligned blueprint fields + wiring graph
- Burst/servo/air piston actuation
- Damage / immobility
- Construction UI

## Pass log (fill after Play)

```
[S3-04] VERIFIER_DONE pass=True nan=False moved=True control_board=True wiring_ok=True mismatches=0 schema=ra2.robot_blueprint.v1 slots=2 wires=8 delta=3.272
```
