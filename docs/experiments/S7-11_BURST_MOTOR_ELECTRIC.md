# S7-11 — BurstMotor Fire electric draw + deny

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-11 BurstMotor Electric)`

## Goal

Prove **BurstMotor Fire** consumes **electric** budget; a second Fire with insufficient remaining power is **denied** (no arc restart).

## Code

| Piece | Role |
|-------|------|
| `RobotActuatorDrive.TriggerFire` | BurstMotor cost 70 elec; `LastElecDenied` |
| `RobotBlueprint.CreateRa2BurstMotorElectricSample` | ElectricTotal=100 (one shot) |
| `RobotBurstMotorElectricVerifier` | Fire rising edges ×2 |

## Pass log

```
[S7-11] VERIFIER_DONE pass=True spent=True fired=True denied=True no_retrigger=True elec_start=100.000 elec_after=30.000 fires=1 elec_denies=1 nan=False
```

## Net notes

- Electric spend is host-authoritative with Fire commands; clients must not invent budget.

## Not in this spike

- Continuous recharge rates
- Shared bus rate limiting beyond thin total
