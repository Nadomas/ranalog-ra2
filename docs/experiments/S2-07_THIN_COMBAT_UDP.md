# S2-07 — Thin Combat Interaction Over UDP

> **Status:** PASS  
> **Date:** 2026-08-21  
> **Depends on:** [S2-06_CROSS_PROCESS_UDP.md](./S2-06_CROSS_PROCESS_UDP.md)  
> **Scene:** same S2-06 Cross-Process UDP scaffold  
> **Maps to:** STAGE 2 deliverable “basic combat interaction” (thin)

## Hypothesis

A remote client can request a thin combat impulse; only the host applies `Rigidbody.AddForce(Impulse)` and sets a disable flag; both peers observe the same authoritative outcome event.

## Method

| Piece | Role |
|-------|------|
| `PhysicsTestCombatRequest` | Client→host intent (attacker/source/target/impulse) |
| `PhysicsTestCombatAuthority` | Host validates source binding, applies impulse, increments hits, disables |
| `PhysicsTestDisableFlag` | Drive refuses forces when disabled |
| `PhysicsTestCombatEvent` | Host→client published outcome |

Not a damage model — impulse + disable only.

## Metrics

```
[S2-07] VERIFIER_DONE pass=True B_disabled=True hits=1 event_disabled=True published_events=1
[S2-07] CLIENT_VERIFIER_DONE pass=True event_seq=1 target=1 disabled=True hits=1
```

Host and client agree: Robot_B disabled after one authoritative hit.

## Verdict

**S2-07 PASS** — thin online combat interaction over the cross-process path is proven for Stage 2 gate language.

## Open questions

- Collision-driven auto-hits vs explicit combat requests for Stage 8 damage.
- How disable maps to modular part lifecycle (Stage 3/7).
