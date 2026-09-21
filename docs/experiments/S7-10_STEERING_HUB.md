# S7-10 — Steering hub Analog (Turn) ±35° + lock

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-10 Steering Hub)`

## Goal

Prove a **Steering** hub responds to Analog Turn (`left_right`) with hinge limits ±35°, drives via motor (no Transform writes), and **locks** at center when Turn≈0.

## Code

| Piece | Role |
|-------|------|
| `RobotAssembler` | Steering visual + hinge limits ±35° |
| `RobotActuatorDrive.TickSteering` | Effort→target angle + lock force |
| `RobotWiringDriveResolver.IsServoActuator` | Steering excluded from wheel motor path |
| `RobotBlueprint.CreateRa2SteeringHubSample` | Tank + steer_hub (no orphan Wheel child) |
| `RobotSteeringHubVerifier` | Turn=1 then Turn=0; angle + lock |

## Pass log

```
[S7-10] VERIFIER_DONE pass=True limits_ok=True steered=True locked=True held=True angle=29.373 lock_w=0.000 steer_locked=1 nan=False
```

## Net notes

- Steering effort from host command bus; clients do not set hinge motors.

## Not in this spike

- Independent front-axle Ackermann geometry polish
- Wheel-on-Steering admit path (would need Axle parent)
- Per-wheel caster / suspension
