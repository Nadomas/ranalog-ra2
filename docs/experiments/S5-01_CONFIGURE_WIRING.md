# S5-01 — Configure Wiring Presets

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S5-01 Configure Wiring)`

## Goal

Prove Configure is a first-class mutation of the shared blueprint control map: rebinding changes physics behaviour without rebuilding the component graph; map serializes with the robot.

## Code

| Piece | Role |
|-------|------|
| `RobotControlConfigurer` | Drive presets + slot bindings + duplicate-wire warnings |
| `RobotConfigureVerifier` | Tank vs Reverse vs TurnOnly deltas + JSON persist |

## Pass log

```
[S5-01] VERIFIER_DONE pass=True tank_delta=2.635 tank_z=-2.625 rev_delta=2.468 rev_z=2.456 turn_only_delta=0.227 reverse_opp=True turn_only_weak=True binding_ok=True wires_ok=True conflicts=0 path=.../ra2_s5_01_configure.json
```

## Not in this spike

- Configure UI chrome  
- Composite groups beyond tank presets  
- Net re-admit of rebound map (use existing Stage 2 command path)

## Next

Stage 6 seamless mode shell.
