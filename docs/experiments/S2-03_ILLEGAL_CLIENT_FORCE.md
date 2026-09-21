# S2-03 — Illegal Client Force / State vs Host Authority

> **Status:** PASS (3 Play Mode auto verifier runs)  
> **Date:** 2026-08-21  
> **Plan:** [S2-00_STAGE2_SPIKE_PLAN.md](./S2-00_STAGE2_SPIKE_PLAN.md)  
> **Depends on:** [S2-02_LOOPBACK_TRANSPORT.md](./S2-02_LOOPBACK_TRANSPORT.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-03 Illegal Client Force)`

## Hypothesis

Illegal client-side Rigidbody/transform mutations (velocity impulse, velocity write, position teleport) on PhysicsTest robots **do not win** against host-owned state: the same command→loopback→authority→drive path remains the only legal motion source, and host reconcile overwrites cheats so authoritative outcome holds (EXP-06 thin). No NGO/NFE package.

## Method

| Piece | Role |
|-------|------|
| Loopback transport + `PhysicsTestLocalAuthority` | Unchanged S2-02 command path |
| `PhysicsTestHostStateAuthority` | Commit all child Rigidbodies after each physics step; re-apply commit at next FixedUpdate (always); count tamper reconciles |
| `PhysicsTestIllegalForceVerifier` | Drive via transport, then apply impulse / velocity / teleport cheats between ticks |

Protocol (auto verifier): A/B forward + dual via transport → unauthorized source reject → direct `SetCommand` overwrite → brake → velocity impulse on A → velocity write on B → transform+RB teleport on A → assert each cheat applied then rejected after host reconcile.

## Metrics (3 runs)

```
[S2-03] VERIFIER_DONE pass=True nan=False A_delta=12.387 B_delta=11.223 both_moved=True
a_not_cheated_away=True reject_ok=True cheat_cmd_ok=True transport_ok=True
impulse_applied=True impulse_rejected=True velocity_applied=True velocity_rejected=True
teleport_applied=True teleport_rejected=True reconciled=True reconciles=3 commits=516
rejected_total=1 applied=516
```

- Console: no project errors after compile / Play Mode.

## Packages

**None added.** Host state reconcile is in-process authority enforcement (stand-in for server correction). Networking solution remains **Open** / `[EXPERIMENT REQUIRED]`.

## Verdict

**S2-03 PASS** — illegal client force/state writes on shared sim bodies are overwritten by host commit/reconcile; legal command→transport→authority→drive path still drives both robots. Ready for **S2-04** (latency harness / feel). Full anti-cheat and cross-process authority still not proven.

## Open questions

- Does always-restore-at-FixedUpdate remain acceptable under latency (S2-04), or do we need soft correction / interpolation?
- How should hinged multi-body restore cost scale once robots have many joints?
- When a real remote client exists, do cheats only hit proxies (no shared RB), making this guard host-only?

## Manual play

- A: WASD + Space (via loopback → host)  
- B: Arrows + Right Shift (via loopback → host)  
- Rebuild menu leaves `autoRun=true` on the verifier; set false for free drive.
