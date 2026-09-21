# S2-05 — Dedicated / Headless Tick Smoke (EXP-09 thin)

> **Status:** PASS (3 Play Mode auto verifier runs) — editor headless smoke only  
> **Date:** 2026-08-21  
> **Plan:** [S2-00_STAGE2_SPIKE_PLAN.md](./S2-00_STAGE2_SPIKE_PLAN.md)  
> **Depends on:** [S2-02_LOOPBACK_TRANSPORT.md](./S2-02_LOOPBACK_TRANSPORT.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-05 Dedicated Tick Smoke)`  
> **Maps to:** EXP-09 thin

## Hypothesis

Authoritative PhysicsTest host can keep both robots moving via loopback→authority→drive with presentation disabled (cameras/listeners/mesh renderers off), without NaN, at an editor-acceptable FixedUpdate cadence. This is **not** a Unity Dedicated Server player build.

## Method

| Piece | Role |
|-------|------|
| `PhysicsTestDedicatedTickVerifier` | Disable presentation; drive 3s; count FixedUpdates / miss ratio |
| Same loopback + authority path as S2-02 | No divergent physics |

## Metrics (3 runs)

```
[S2-05] VERIFIER_DONE pass=True nan=False both_moved=True
A_delta≈15.02 B_delta≈14.67 fixed_ticks≈99 expected≈150
avg_tick_ms≈30.3 miss_ratio≈0.02 transport_ok=True presentation=disabled
```

- Wall-clock FixedUpdate spacing (~30 ms) reflects Editor Play Mode frame pacing, not CPU ms/tick of a headless server process.
- Console: no project errors.

## Packages

**None added.** Dedicated Server approach remains **Open** / `[EXPERIMENT REQUIRED]`.

## Verdict

**S2-05 PASS (thin)** — presentation-disabled host tick smoke is stable enough for 1v1 PhysicsTest complexity in Editor. **True Dedicated Server build + CPU budget profiling still required** before claiming EXP-09 complete.

## Open questions

- When to add a real `-batchmode` / Dedicated Server player build spike?
- How does tick cost scale with Stage 3 modular multi-body robots?
