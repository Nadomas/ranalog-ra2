# S7-01 / S8-01 — Functional Disable + Immobility Win

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S8-01 Combat Immobility)`  
> **Covers:** Stage 7 thin (disable) + Stage 8 thin (1v1 Immobilized outcome)

## Goal

Prove combat spine for MVP: damage as functional disable → immobility countdown → authoritative win reason `Immobilized` (no Transform teleport; Rigidbody velocity kill on disable only).

## Code

| Piece | Role |
|-------|------|
| `RobotDamageService` | Functional disable/enable; stop residual dynamics via RB velocities |
| `ImmobilityWinEvaluator` | Plain C# match rules; disabled fighters always accrue immobile time |
| `RobotCombatImmobilityVerifier` | Dual spawn → disable A → B wins Immobilized |

## Pass log

```
[S8-01] VERIFIER_DONE pass=True disabled=True a_stuck=True finished=True reason=Immobilized winner=1 loser=0 immobileA=1.22 immobileB=0.00
```

## Not in this spike

- Weapon hit formulae / chassis splash  
- Detach net debris  
- Arena art / lobby / results UI  
- Full Stage 9 session flow

## Next

Stage 9 thin lobby→fight→results on UDP path, or Stage 10 results summary stub — then Stage 11 integration glue.
