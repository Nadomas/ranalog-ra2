# S5-00 — STAGE 5 Spike Plan (Control / Configuration)

> **Date:** 2026-09-21  
> **Status:** S5-01 **PASS**  
> **Depends on:** Stage 3 wiring + Stage 4 construction samples

## Spike sequence

| ID | Hypothesis | Status | In scope | Out of scope |
|----|------------|--------|----------|--------------|
| **S5-01** | Rebind wiring/presets changes drive; map persists in blueprint JSON | **PASS** | presets, slot bindings, conflict warn, Play verify | Full Configure UI, all devices |
| **S5-02** | (optional) Net-executed control map on UDP path | deferred | Stage 2 command bus already carries drive cmds | Full lobby |

## S5-01 success

- `RobotControlConfigurer` applies TankSteer / ReversedDrive / TurnOnly presets.  
- Same hardware + ForwardBack=+1 moves opposite under reverse wiring.  
- TurnOnly collapses forward travel.  
- Bindings persist through `ra2.robot_blueprint.v1` JSON.

## Pass log

```
[S5-01] VERIFIER_DONE pass=True tank_delta=2.635 tank_z=-2.625 rev_delta=2.468 rev_z=2.456 turn_only_delta=0.227 reverse_opp=True turn_only_weak=True binding_ok=True wires_ok=True conflicts=0
```

## Next

Stage 6 seamless Design ↔ Configure ↔ Test mode shell (in-memory robot retain).
