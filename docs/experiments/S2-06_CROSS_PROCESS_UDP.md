# S2-06 — Cross-Process UDP Transport (EXP-05)

> **Status:** PASS  
> **Date:** 2026-08-21  
> **Plan:** [S2-00_STAGE2_SPIKE_PLAN.md](./S2-00_STAGE2_SPIKE_PLAN.md)  
> **Depends on:** [S2-02_LOOPBACK_TRANSPORT.md](./S2-02_LOOPBACK_TRANSPORT.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S2-06 Cross-Process UDP)`  
> **Player:** `Tools/RA2/Build PhysicsTest Windows Player (S2 net)`  
> **Maps to:** EXP-05 (real peers), EXP-07 over sockets

## Hypothesis

Commands and thin pose snapshots can cross an **OS process boundary** via provisional localhost UDP while still driving the same host authority → `PhysicsTestDrive` path (no Transform cheat, prediction OFF).

## Method

| Process | Role |
|---------|------|
| Unity Editor Play Mode | Listen-host: PhysX + `PhysicsTestLocalAuthority` + `PhysicsTestUdpHost` |
| Windows player build | Remote client: `-ra2-role=Client`; strips authoritative RBs; sends cmds via UDP |

Protocol: binary hello / command / pose batch (`PhysicsTestUdpCodec`). No NGO / Unity Transport package.

## Metrics

```
[S2-06] VERIFIER_DONE pass=True A_delta≈12.701 B_delta≈14.214 peer=True
delivered_cmds=150 drained=150 snaps=130 hello=1
[S2-06] CLIENT_VERIFIER_DONE pass=True cmds=150 snaps=153
```

- Console: no project compile/runtime errors on smoke.

## Packages

**None added.** Provisional transport candidate = custom UDP listen-host. Remains `[EXPERIMENT REQUIRED]` (not frozen).

## Verdict

**S2-06 PASS** — in-process loopback is no longer the only proven path; cross-process command + pose sync works for PhysicsTest 1v1.

## Open questions

- When does custom UDP need reliable/ordered channels vs raw datagrams?
- Should NGO/UTP replace this once modular sync surface is known (Stage 3)?
