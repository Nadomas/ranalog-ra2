# S3-03 — Host Net Spawn from Blueprint (loopback thin)

> **Status:** PASS (3 Play Mode auto verifier runs)  
> **Date:** 2026-08-21  
> **Plan:** [S3-00_STAGE3_SPIKE_PLAN.md](./S3-00_STAGE3_SPIKE_PLAN.md)  
> **Depends on:** [S3-01_MODULAR_ASSEMBLY.md](./S3-01_MODULAR_ASSEMBLY.md), [S3-02_BLUEPRINT_SERIALIZE.md](./S3-02_BLUEPRINT_SERIALIZE.md), Stage 2 loopback  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S3-03 Net Spawn)`

## Hypothesis

An empty arena can admit sample blueprints over the Stage 2 **in-process loopback** boundary; the **host** validates schema, assembles via the **shared** `RobotSpawnService` → `RobotAssembler` path, binds authority/drive, and publishes spawn/despawn events so the client receives the **same logical component graph** — then drive + thin poses work without Transform cheats.

## Method

| Piece | Role |
|-------|------|
| `RobotSpawnService` | Shared local/net spawn+despawn (assemble + `PhysicsTestDrive`) |
| `RobotSpawnRequest` / `RobotSpawnEvent` | Thin loopback payloads (`ra2.robot_blueprint.v0` JSON) |
| `PhysicsTestLoopbackTransport` | Spawn request/event channels (+ existing commands/poses) |
| `RobotHostSpawner` | Host validate → spawn → rebind authority → publish events |
| `RobotNetSpawnVerifier` | Admit A/B → drive smoke → despawn B |

Out of scope: cross-process / NGO, client physics assembly, detach/disable lifecycle, catalog, Construction UI.

## Metrics (3 runs)

```
[S3-03] VERIFIER_DONE pass=True nan=False both_moved=True hinge_ok=True
graph_ok=True transport_ok=True pose_ok=True despawn_ok=True assemble_budget_ok=True
A_delta≈6.18 B_delta≈5.59 assemble_ms_max≈11.2 accepted_spawns=2 accepted_despawns=1
spawn_reqs=3 spawn_evts=3 schema=ra2.robot_blueprint.v0 spawned_from=blueprint_loopback
```

- Console: no project errors after compile / Play Mode.  
- First-assemble ~11 ms; subsequent ~0.9 ms (budget check `< 50 ms`).

## Packages

**None added.** Still Stage 2 loopback; NGO/NFE not frozen.

## Verdict

**S3-03 PASS** — host net-spawn from modular blueprint on loopback is stable for the PhysicsTest samples. This is the thin **spawn-in-net demo** for Stage 3. Cross-process admit closed separately in [S2-09_MODULAR_UDP_SPAWN.md](./S2-09_MODULAR_UDP_SPAWN.md) (**PASS**).

## Open questions

- Client presentation assemble vs logical-graph-only until real transport.  
- Detach/disable lifecycle vs create/despawn-only.  
- Whether match admit should carry blueprint bytes or content-addressed ids.
