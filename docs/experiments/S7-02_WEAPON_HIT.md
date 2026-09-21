# S7-02 — Weapon Hit Apply Thin (concussion / piercing)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-02 Weapon Hit)`  
> **Covers:** Stage 7 optional thin weapon hit → degrade → functional disable (host apply)

## Goal

Prove the thinnest host-applied weapon hit path using GDD concussion+piercing mix × impact proxy, feeding the existing disable → immobility spine — no weapons catalog, no chassis splash polish, no detach.

## Formula (thin)

```
severity = impact × mean(concussion, piercing) × (1 − armorAbsorb)
```

| Accumulated severity | Outcome |
|----------------------|---------|
| &lt; 0.5 | None (intact drive scale) |
| ≥ 0.5 | Degraded (`DrivePowerScale = 0.35`) |
| ≥ 1.0 | Functional disable via `RobotDamageService.TryFunctionalDisable` |

## Code

| Piece | Role |
|-------|------|
| `RobotWeaponHit` / `RobotWeaponHitOutcome` | Thin hit request + outcome enum |
| `RobotDamageService.ComputeHitSeverity` / `TryApplyWeaponHit` | Host/sim plain C# apply |
| `PhysicsTestDrive.DrivePowerScale` / `RobotMotorDrive.DrivePowerScale` | Degrade multiplier (forces/motors) |
| `RobotWeaponHitVerifier` | Weak → mid degrade → kill disable + stuck check |

## Authority

- Apply path is host/sim service only (same contract as S7-01 disable).
- Clients must not decide degrade/disable outcomes.
- No Transform teleport; disable still kills residual RB velocity.

## Pass log

```
[S7-02] VERIFIER_DONE pass=True weak=True degrade=True disabled=True stuck=True accum=1.33 weak_sev=0.23 mid_sev=0.55 kill_sev=0.55 scale=0.00
```

## Not in this spike

- Weapons catalog / arena art  
- Chassis splash distance field  
- Exact RA2 closed-form impact×armor  
- Partial per-component HP curves  
- Net replication of damage events beyond existing combat UDP

## Next

Stage 9–11 thin residuals are already PASS; remaining open queue items are human/meta (`PIPE-T02`, `PIPE-T03`) or deferred polish (`S04-T02`, `S05-T02`).
