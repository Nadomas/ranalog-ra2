# STAGE 2 — GO / NO-GO Recommendation

> **Date:** 2026-09-21 (re-verified; original closure 2026-08-21)  
> **Verdict:** **GO** — HARD GATE closed for **non-loopback** UDP authority + **modular UDP spawn**  
> **Spikes:** S2-01…S2-05 ([S2-00 plan](./S2-00_STAGE2_SPIKE_PLAN.md)) + **S2-06 / S2-07 / S2-08** + **S2-09**

---

## 1. What was proven (evidence)

| Spike | Result | Evidence |
|-------|--------|----------|
| S2-01 Local authority command bus | PASS | Dual robots only via host bus → `PhysicsTestDrive` |
| S2-02 Loopback transport | PASS | Commands + thin poses cross in-process boundary |
| S2-03 Illegal client force | PASS | Impulse / velocity / teleport overwritten by host reconcile |
| S2-04 Latency harness | PASS | Playable under 60–100 ms RTT + jitter/5% loss; **predict OFF** |
| S2-05 Dedicated tick smoke | PASS thin | Presentation-disabled Editor host tick |
| **S2-06 Cross-process UDP** | **PASS** (re-verified 2026-09-21) | Editor host + Windows player client over localhost UDP; cmds/poses agree |
| **S2-07 Thin combat online** | **PASS** (re-verified 2026-09-21) | Client combat request → host impulse + disable flag; both peers agree |
| **S2-08 Headless player tick** | **PASS** | `-batchmode -nographics` player build; `player_build=True`, miss_ratio=0 |
| **S2-09 Modular Cross-Process UDP** | **PASS** (2026-09-21) | Empty arena → UDP blueprint admit → host assemble → drive + combat disable |

Shared invariants held across spikes:

- No Transform-drive on dynamics for locomotion  
- Command → (transport) → authority → drive separation  
- Same PhysicsTest physical model (no fake net physics)  
- **Prediction OFF**  
- **No NGO/NFE package** — provisional candidate is **custom UDP listen-host** (`[EXPERIMENT REQUIRED]` until production freeze)

### Cross-process metrics (S2-06 / S2-07) — re-verify 2026-09-21

```
[S2-06] VERIFIER_DONE pass=True A_delta≈12.69 B_delta≈14.20 peer=True
delivered_cmds=154 drained=154 snaps=129 combat_applied=1 B_disabled=True
[S2-07] VERIFIER_DONE pass=True B_disabled=True hits=1 event_disabled=True
[S2-06] CLIENT_VERIFIER_DONE pass=True snaps=152 combat_events=1 target_disabled=True
```

### Modular UDP spawn metrics (S2-09) — 2026-09-21

```
[S2-09] VERIFIER_DONE pass=True live=2 accepted=2 rejected=1
A_delta≈5.90 B_delta≈4.14 hinge_ok=True delivered_cmds=202 drained=202
spawn_reqs=3 spawn_evts=3 dropped_spawn=0 combat_applied=1 B_disabled=True
assemble_ms_max≈12.6
[S2-09] CLIENT_VERIFIER_DONE pass=True graphs=2 rejects=1
reject_reason=unsupported_schema combat_events=1 target_disabled=True
```

See [S2-09_MODULAR_UDP_SPAWN.md](./S2-09_MODULAR_UDP_SPAWN.md).

### Headless player metrics (S2-08)

```
[S2-08] VERIFIER_DONE pass=True both_moved=True fixed_ticks=202 expected≈200
avg_wall_tick_ms≈19.86 miss_ratio=0.000 player_build=True role=Dedicated
```

---

## 2. Residual (non-blocking for HARD GATE)

| Item | Status | Notes |
|------|--------|-------|
| Unity **Dedicated Server** Win module / `StandaloneBuildSubtarget.Server` | Not installed on this machine | Re-tried 2026-09-21: *“Dedicated Server support for Win is not installed.”* Headless **Standalone player** smoke used for EXP-09 evidence. |
| NGO / Unity Transport / NFE | Not chosen | Custom UDP is provisional candidate only — not frozen |
| Production composite ActionCommand path | Open (ARCH C1) | Stage 2 used thin drive envelopes; composites still Stage 5 risk |
| Sync surface under larger modular robots | Watch (ARCH C2/C3) | S2-09 closed cross-process admit for PhysicsTest samples; production budget still open |

---

## 3. Recommendation

**HARD GATE GO** on the proven path:

1. Cross-process authoritative 1v1 PhysicsTest works (UDP listen-host).  
2. Thin combat outcome (disable) agrees on host + remote client.  
3. **Modular blueprint spawn** works on the **same UDP path** (empty arena → host validate/assemble → rebind → drive/combat).  
4. Cheat/authority + latency lessons from S2-03/04 still hold; prediction stays OFF.  
5. Headless player-build tick budget is green for 1v1 demo complexity.

**HARD STOP still applies to:** Construction polish, content catalog, economy, career — until later stages prove them on this net+physics base.

---

## 4. EXP status snapshot

| EXP | Status |
|-----|--------|
| EXP-05 sync physics | **PASS** (cross-process UDP thin poses + agreed combat + modular spawn) |
| EXP-06 authority | **PASS** (S2-01/03 + host combat authority + spawn rebind) |
| EXP-07 input sync | **PASS** (envelopes over UDP) |
| EXP-08 latency | **PASS** (predict OFF; S2-04) |
| EXP-09 dedicated | **PASS (headless player)**; true Server subtarget blocked until module install |

---

## 5. Provisional net approach (not frozen)

| Layer | Candidate | Status |
|-------|-----------|--------|
| Authority | Listen-host / dedicated sim; clients send commands + combat/spawn requests only | Provisional |
| Transport | Custom localhost UDP (`PhysicsTestUdpTransport`) | `[EXPERIMENT REQUIRED]` candidate |
| Spawn | `RobotHostSpawnerUdp` + shared `RobotSpawnService` | Proven thin (S2-09) |
| Packages | None added for Stage 2 closure | Prefer keep until UDP limits proven |

---

*This document is the Stage 2 HARD GATE record. Non-loopback + modular UDP spawn claimed GO as of 2026-09-21.*
