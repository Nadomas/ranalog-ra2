# S2-02 — Provisional Loopback Transport

> **Status:** PASS (3 Play Mode auto verifier runs)  
> **Date:** 2026-08-21  
> **Plan:** [S2-00_STAGE2_SPIKE_PLAN.md](./S2-00_STAGE2_SPIKE_PLAN.md)  
> **Depends on:** [S2-01_LOCAL_AUTHORITY_COMMAND_BUS.md](./S2-01_LOCAL_AUTHORITY_COMMAND_BUS.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-02 Loopback Transport)`

## Hypothesis

Commands can cross a **provisional transport boundary** (in-process listen-host loopback) and still drive the same S2-01 host authority → `PhysicsTestDrive` path, while thin host→client pose snapshots remain consistent enough for a logical client view. No NGO/NFE package required for this question.

## Method

| Peer | Role |
|------|------|
| `LoopbackClient` + `PhysicsTestTransportClient` | Logical client: enqueue `PhysicsTestCommandEnvelope`; drain thin poses |
| `ListenHost` + `PhysicsTestTransportHost` | Drain queue → `PhysicsTestLocalAuthority.Submit`; publish pose snapshots |
| `PhysicsTestLocalAuthority` | Sole writer of `PhysicsTestDrive.SetCommand` (unchanged from S2-01) |
| Keyboard sources | Prefer transport client when configured (same path as remote) |

Protocol (auto verifier): A forward → B forward → dual drive via transport → unauthorized sourceId → direct cheat `SetCommand` then authority idle overwrite → compare client LastPoses to host transforms.

## Metrics (3 runs)

```
[S2-02] VERIFIER_DONE pass=True nan=False A_delta≈10.68 B_delta=12.270 both_moved=True
reject_ok=True cheat_overwritten=True transport_ok=True pose_ok=True
enqueued=371 delivered=371 drained=371 snaps=188 rejected_total=1 applied=376
```

- Console: no project errors after compile / Play Mode.

## Packages

**None added.** Transport is a thin in-process queue with explicit envelope/pose copies (stand-in for future RPC payload). Networking solution remains **Open** / `[EXPERIMENT REQUIRED]`.

## Verdict

**S2-02 PASS** — commands cross a loopback transport boundary and still drive authoritative PhysicsTest sim; thin pose snapshots match host transforms within tolerance; S2-01 reject/overwrite smoke still holds. Ready for **S2-03** (illegal client force/state does not win). Cross-process sockets / NGO still not proven (deferred).

## Open questions

- When is a real socket/NGO layer required vs extending this loopback abstraction?
- Does thin pose sync stay enough under latency (S2-04), or do we need prediction/interpolation?
- How should multi-client fan-out of snapshots look once there are two OS processes?

## Manual play

- A: WASD + Space (via loopback → host)  
- B: Arrows + Right Shift (via loopback → host)  
- Rebuild menu leaves `autoRun=true` on the verifier; set false for free drive.
