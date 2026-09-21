# S7-08 — ServoPiston Analog Extend/Retract (air)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-08 ServoPiston Analog)`

## Goal

Prove **ServoPiston** maps Analog −1…+1 to slider stroke via **ConfigurableJoint** `xDrive` / `targetPosition`, drains **AirTank** while actuating, and **locks mid-stroke** when Analog returns to 0 — no Transform teleport.

## Code

| Piece | Role |
|-------|------|
| `RobotJointKind.Slider` | Linear actuator (shared with BurstPiston) |
| `RobotActuatorDrive` | Servo stroke drive + air/sec + mid lock |
| `RobotBlueprintValidator` | ServoPiston requires AirTank + AirTotal |
| `RobotBlueprint.CreateRa2ServoPistonAnalogSample` | Tank + airtank + servo piston |
| `RobotServoPistonAnalogVerifier` | Extend → lock mid → retract |

## Pass log

```
[S7-08] VERIFIER_DONE pass=True extended=True mid_held=True retracted=True air_start=600.000 air_after=549.401 air_now=498.802 air_spent=True ext=0.450 mid=0.446 ret=-0.001 denies=0 nan=False
```

## Net notes

- Air remaining is host/local sim state.
- Joint target X is sign-inverted vs local axis Dot so +Extend matches +extension measure.

## Not in this spike

- Air recharge / rate limiting polish
- Burst vs Servo shared catalog tuning
- Empty-air deny while mid-hold (thin continues hold)

## Next

S7-09 SmartZone contact → optional Fire.
