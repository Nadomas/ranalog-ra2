# S7-14 — Ackermann steer + wheel admit

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-14 Ackermann Steer)`

## Goal

Prove front **Ackermann** graph admits: `Chassis → Steering → SpinMotor → Wheel`, opposite Turn angles on L/R hubs, wheel hinge connected to steer RB (not always chassis root).

## Code

| Piece | Role |
|-------|------|
| `RobotBlueprintValidator` | SpinMotor may parent under Steering |
| `RobotAssembler.ResolveConnectedBody` | Hinge/slider to nearest parent RB |
| `RobotAssembler` | Pre-create HasRigidbody bodies before joints |
| `RobotBlueprint.CreateRa2AckermannSteerSample` | Dual front steers + motors/wheels |
| `RobotAckermannSteerVerifier` | admit + opposite angles + move |

## Pass log

```
[S7-14] VERIFIER_DONE pass=True admit=True opposite=True wheel_on_steer=True moved=True a_fl=34.718 a_fr=-34.515 steers=2 nan=False
```

## Net notes

- Steering + drive remain host command path.

## Not in this spike

- True geometric Ackermann toe tables  
- Rear steer / 4WS  
- Suspension
