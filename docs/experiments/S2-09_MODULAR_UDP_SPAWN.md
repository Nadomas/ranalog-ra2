# S2-09 — Modular Blueprint Spawn over Cross-Process UDP

> **Date:** 2026-09-21  
> **Status:** **PASS**  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-09 Modular Cross-Process UDP)`  
> **Player:** `Tools/RA2/Build PhysicsTest Windows Player (S2 net)` + `-ra2-role=Client`

---

## Hypothesis

Stage 3 loopback host spawn (`RobotHostSpawner` + `PhysicsTestLoopbackTransport`) can ride the **same UDP path** proven in S2-06/S2-07: client admits blueprints → host validates/assembles via shared `RobotSpawnService` → command→authority→drive + thin combat disable, with **no Transform cheat** and **prediction OFF**.

## Spike shape

| Piece | Role |
|-------|------|
| `PhysicsTestUdpCodec` MsgSpawnRequest/Event | Binary encode; reject >48 KiB |
| `PhysicsTestUdpTransport` | Queue/drain spawn payloads over localhost UDP |
| `RobotHostSpawnerUdp` | Host validate → assemble → rebind authority/host/combat |
| `RobotModularUdpHostVerifier` / `RobotModularUdpClientVerifier` | Cross-process host + client smoke |
| Empty arena scene | No pre-placed robots |

## Success criteria

1. Editor host + Windows player client over UDP  
2. Valid A/B blueprints accepted; unsupported schema rejected  
3. Client graph from spawn events matches host assemble  
4. Both peers agree on motion via thin poses  
5. Thin combat disable agrees (S2-07 path after spawn rebind)  

## Evidence (2026-09-21)

Host (Editor):

```
[S2-09] VERIFIER_DONE pass=True reason=ok live=2 accepted=2 rejected=1
A_delta=5.897 B_delta=4.141 hinge_ok=True peer=True
delivered_cmds=202 drained=202 snaps=123 spawn_reqs=3 spawn_evts=3
dropped_spawn=0 combat_applied=1 B_disabled=True assemble_ms_max=12.609
[S2-09] COMBAT_DONE pass=True B_disabled=True hits=1 event_disabled=True
```

Client (Windows player):

```
[S2-09] CLIENT_VERIFIER_DONE pass=True reason=ok graphs=2 rejects=1
reject_reason=unsupported_schema cmds=202 spawn_reqs=3 spawn_evts=3
snaps=146 combat_events=1 target_disabled=True dropped_spawn=0
```

## Notes

- Loopback `RobotHostSpawner` remains for S3-03 in-process spikes; UDP path is `RobotHostSpawnerUdp`.  
- Oversized spawn payloads are dropped (`MaxSpawnPayloadBytes = 48 KiB`).  
- Custom UDP still `[EXPERIMENT REQUIRED]` — not a production freeze.
