# S2-08 — Dedicated / Headless Player Build Smoke (EXP-09)

> **Status:** PASS (headless Standalone player) — Unity Dedicated Server Win module not installed  
> **Date:** 2026-08-21  
> **Depends on:** [S2-05_DEDICATED_TICK_SMOKE.md](./S2-05_DEDICATED_TICK_SMOKE.md), S2-06 scaffold  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-08 Dedicated Build Smoke)`  
> **Maps to:** EXP-09

## Hypothesis

A **player build** can run authoritative PhysicsTest host tick without rendering (`-batchmode -nographics`) at a stable FixedUpdate cadence for 1v1 demo complexity.

## Method

| Attempt | Result |
|---------|--------|
| `StandaloneBuildSubtarget.Server` | **Failed** — *Dedicated Server support for Win is not installed* |
| Windows player + `-batchmode -nographics -ra2-role=Dedicated` | **PASS** |

Verifier: `PhysicsTestDedicatedBuildVerifier` (presentation disabled, authority drive both robots, CPU/wall tick metrics).

## Metrics

```
[S2-08] VERIFIER_DONE pass=True both_moved=True fixed_ticks=202 expected≈200
avg_wall_tick_ms≈19.86 cpu_ms_per_tick≈19.86 miss_ratio=0.000
player_build=True role=Dedicated headless_like=True
```

## Packages

**None added.**

## Verdict

**S2-08 PASS** for EXP-09 gate evidence via headless Standalone player. Install Unity **Dedicated Server (Windows)** module before claiming true server-subtarget builds / multi-match hosting cost model.

## Open questions

- Install DS module and re-run `Tools/RA2/Build PhysicsTest Dedicated Server (S2-08)`.
- Tick cost under Stage 3 modular multi-body robots.
