# S7-13 — Electric recharge (ElectricMaxInOutRate)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-13 Electric Recharge)`

## Goal

Prove electric budget **refills** over time from `Power.ElectricMaxInOutRate` (capped by positive Battery `ElecMaxInOutRate`) after BurstMotor Fire spend, enabling a second Fire without respawn.

## Code

| Piece | Role |
|-------|------|
| `RobotActuatorDrive.TickElectricRecharge` | FixedUpdate refill toward ElectricTotal |
| `RobotBlueprint.CreateRa2ElectricRechargeSample` | ElectricTotal=100, rate=250 |
| `RobotElectricRechargeVerifier` | Fire → wait → recharge → Fire again |

## Pass log

```
[S7-13] VERIFIER_DONE pass=True spent=True recharged=True second_fire=True elec_start=100.000 elec_after_fire=35.000 elec_after_recharge=100.000 fires=2
```

## Net notes

- Recharge is host/local sim; clients must not invent electric remaining.

## Not in this spike

- Arena charge pads / hazards
- Nonlinear battery curves
- Continuous motor draw vs burst Fire accounting polish
