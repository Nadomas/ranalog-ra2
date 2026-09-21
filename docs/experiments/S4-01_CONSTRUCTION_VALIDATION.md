# S4-01 — Construction Validation (mass/CoM + attachment + admit)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S4-01 Construction Validation)`  
> **Depends on:** [S4-00](./S4-00_STAGE4_SPIKE_PLAN.md), Stage 3 modular runtime

## Goal

Prove construction rules on the shared v1 blueprint: illegal builds fail admit; ≥2 valid builds with distinct mass/CoM spawn and drive on the shared path.

## Code

| Piece | Role |
|-------|------|
| `RobotMassProperties` | Total mass + CoM from component masses; apply to root Rigidbody |
| `RobotBlueprintValidator` | chassis ≤16, wheel→motor axle (v1), battery/electric, mass class cap, rb/hinge budgets |
| `RobotBlueprint.CreateRa2ConstructionSampleA/B` | Two valid builds (B rear ballast → different CoM) |
| Invalid factories | wheel-on-chassis, >16 pts, overmass, no Control Board |
| `RobotSpawnService` | Applies CoM after assemble (shared admit/spawn) |
| `RobotConstructionValidatorVerifier` | Validate + reject + dual spawn/drive |

## Pass log

```
[S4-01] VERIFIER_DONE pass=True nan=False both_moved=True com_distinct=True com_applied=True axles=True rejects=True A_mass=21.70 A_comZ=-0.014 B_mass=29.70 B_comZ=-0.266 A_delta=2.469 B_delta=2.317 rb=5 hinges=4
```

## Not in this spike

- Construction editor UX / polygon drawing UI  
- Catalog / economy  
- Mesh-perfect overlap (AABB warnings only)  
- Frozen mass-class numbers

## Next

Stage 5 thin Configure: rebind wiring on blueprint → different drive behaviour on same hardware.
