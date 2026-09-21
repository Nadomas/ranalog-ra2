# EXP-02 — Bodies / Joints Budget (provisional)

> **Status:** baseline only (no stress ladder yet)  
> **Date:** 2026-08-21  
> **Source:** PhysicsTest S1-03 hinge spike

## Measured baseline (1 demo machine)

| Scope | Rigidbodies | Joints |
|-------|-------------|--------|
| Robot_A (S1-03) | 2 | 1 |
| Robot_B (passive compound) | 1 | 0 |
| Scene robots total | 3 | 1 |

No overt fixed-timestep overrun observed on this demo (qualitative; no formal profiler gate).

## Provisional yellow thresholds (until stress EXP-02)

Conservative placeholders for STAGE 2 planning — **not** a freeze:

| Scope | Yellow (investigate) | Notes |
|-------|----------------------|--------|
| Per robot | \> **20** bodies or \> **15** joints | Spike only proved 2/1; ceiling unknown |
| 1v1 match | \> **40** bodies or \> **30** joints | Sum of both machines + arena static ignored |

Green for STAGE 1 demo: current PhysicsTest complexity.

## Next (not STAGE 1)

Step ladder: 4 hinged wheels → N joints → profile `Physics.Processing` / fixed overruns → replace yellow numbers with measured ceilings.
