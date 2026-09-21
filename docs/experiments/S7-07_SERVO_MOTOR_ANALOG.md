# S7-07 — ServoMotor Analog (slow + lock)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-07 ServoMotor Analog)`

## Goal

Prove **ServoMotor** responds to **Analog** effort with **slow** hinge motion toward a target angle (±90° limits) and **locks at stop** (high motor force, targetVelocity=0) when analog is released — no Transform writes on dynamics.

## Code

| Piece | Role |
|-------|------|
| `RobotAssembler` | ServoMotor hinge limits −90…90° |
| `RobotMotorDrive` | Skips Servo* (position lock is actuator path) |
| `RobotActuatorDrive` | Analog → slow chase + lock deadzone |
| `RobotBlueprint.CreateRa2ServoMotorAnalogSample` | Tank + servo + arm + Analog slot |
| `RobotServoMotorAnalogVerifier` | Move→angle; release→lock hold |

## Pass log

```
[S7-07] VERIFIER_DONE pass=True limits_ok=True moved=True locked=True held=True angle=64.356 angle_lock=64.446 drive_w=1.306 lock_w=0.000 servo_locked=1 nan=False
```

## Net notes

- Servo target from authoritative command Analog (Move axis for dedicated Analog slots).
- Angle result is physics; do not client-author hinge pose.

## Not in this spike

- Electric budget draw while slewing
- Multi-turn continuous servos
- Catalog servo defs / torque curves

## Next

S7-08 ServoPiston Analog Extend/Retract (air).
