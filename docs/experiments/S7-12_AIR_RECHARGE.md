# S7-12 — Air tank recharge (AirMaxInOutRate)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-12 Air Recharge)`

## Goal

Prove air budget **refills** over time from `Power.AirMaxInOutRate` (capped by positive AirTank component rates) after BurstPiston Fire spend, enabling a second Fire without respawn.

## Code

| Piece | Role |
|-------|------|
| `RobotActuatorDrive.TickAirRecharge` | FixedUpdate refill toward AirTotal |
| `RobotBlueprint.CreateRa2AirRechargeSample` | AirTotal=100, rate=250 |
| `RobotAirRechargeVerifier` | Fire → wait → recharge → Fire again |

## Pass log

```
[S7-12] VERIFIER_DONE pass=True spent=True recharged=True second_fire=True air_start=100.000 air_after_fire=25.000 air_after_recharge=100.000 fires=2
```

## Net notes

- Recharge is host/local sim; clients must not invent air remaining.

## Not in this spike

- Electric recharge mirror
- Compressor hazards / arena air pads
- Nonlinear tank curves
