# S2-04 — Latency Harness (RTT / Jitter / Loss)

> **Status:** PASS (3 Play Mode auto verifier runs)  
> **Date:** 2026-08-21  
> **Plan:** [S2-00_STAGE2_SPIKE_PLAN.md](./S2-00_STAGE2_SPIKE_PLAN.md)  
> **Depends on:** [S2-02_LOOPBACK_TRANSPORT.md](./S2-02_LOOPBACK_TRANSPORT.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-04 Latency Harness)`  
> **Maps to:** EXP-08

## Hypothesis

Simulated RTT/jitter/loss on the in-process loopback transport still yields playable authoritative drive (both robots move, no NaN), with measurable input→motion lag tracking ~RTT/2. Presentation interpolation may help remote pose smoothness; **prediction can stay OFF** for this complexity.

## Method

| Piece | Role |
|-------|------|
| `PhysicsTestLatencyProfile` | RTT / jitter / loss / interpolate flags |
| `PhysicsTestLoopbackTransport` | Delayed + droppable command/snapshot queues |
| `PhysicsTestTransportClient` | Optional presentation-only pose lerp (no RB writes) |
| `PhysicsTestLatencyVerifier` | Sweep profiles; measure lag, pose error, motion |

Profiles: 0 ms → 60 ms RTT → 100 ms RTT + 5% loss (raw) → same with interpolate.

## Metrics (3 runs, representative)

```
[S2-04] VERIFIER_DONE pass=True nan=False profiles_ok=True
baseline_A_delta≈3.22 worst_pose_err≈0.007 worst_input_lag_ms≈60
raw_max_step≈0.08 interp_max_step≈0.15 pose_budget_ok=True lag_ok=True
predict=OFF recommend_interp=False recommend_predict=False

p0 rtt=0   lag≈15–20 ms  A_delta≈3.22
p1 rtt=60  lag≈40 ms     avg_delay_ms≈29.7
p2 rtt=100 loss=0.05 lag≈60 ms drop_cmd≈9/169 avg_delay_ms≈50
p3 rtt=100+interp pass=True
```

- Console: no project errors after compile / Play Mode.

## EXP-08 decisions

| Question | Decision |
|----------|----------|
| Playable at ≤100 ms RTT (loopback harness)? | **Yes** — motion + authority path hold |
| Need prediction now? | **No** — keep OFF; modular joints make prediction expensive (ARCH C3) |
| Need interpolation now? | **Optional later** for remote presentation when poses are truly delayed across processes; not required on shared-sim loopback (pose_err ≈ 0 after drain) |
| NGO / netcode freeze? | **Still Open** / `[EXPERIMENT REQUIRED]` |

## Packages

**None added.**

## Verdict

**S2-04 PASS** — latency harness proves command→authority→drive remains usable under simulated 60–100 ms RTT with jitter/loss; predict OFF; interp deferred. Ready for **S2-05** (dedicated/headless tick smoke).

## Open questions

- Cross-process snapshot lag will show real pose error; re-evaluate interp then.
- Soft correction vs hard host reconcile under latency (S2-03 open question) still pending remote clients.
