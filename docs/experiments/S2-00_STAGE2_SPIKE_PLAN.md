# S2-00 — STAGE 2 Spike Plan (Multiplayer Physics Prototype)

> **Date:** 2026-08-21  
> **Status:** Complete (S2-01…S2-08) — see [STAGE2_GO_NOGO.md](./STAGE2_GO_NOGO.md) **GO (HARD GATE closed)**  
> **Depends on:** [STAGE1_EXIT.md](./STAGE1_EXIT.md), [UNITY_ENGINE.md](../UNITY_ENGINE.md) EXP-05…09, [TECHNICAL_ROADMAP.md](../TECHNICAL_ROADMAP.md) STAGE 2  
> **Architecture caution:** [ARCHITECTURE_REVIEW.md](../ARCHITECTURE_REVIEW.md) C1 (shared command path), C2 (provisional GATE), C3 (sync surface ≠ local fidelity)

---

## 1. STAGE 2 goal (roadmap)

Prove **physics + multiplayer** are viable: 2 clients, 2 robots, movement/collisions sync, **server-authoritative** sim, input → server commands. Later HARD GATE; this plan only sequences spikes.

## 2. Non-negotiable constraints

| Constraint | Implication |
|------------|-------------|
| Do **not** freeze NGO / NFE / transport | Candidates remain `[EXPERIMENT REQUIRED]` until EXP-05…08 green |
| Same physical model as PhysicsTest | No divergent “fake net physics” |
| Input ≠ authority | Devices → commands → host/server applies forces |
| MonoBehaviour = glue | Command bus / validation stay plain C# where practical |
| No premature packages | Add Netcode only when a spike explicitly requires it; document provisional |

## 3. Candidate net approach (provisional — not frozen)

| Layer | Provisional choice | Status |
|-------|--------------------|--------|
| Authority | Server / listen-host simulates PhysX; clients send commands only | Target model |
| Transport / API | **Undecided** among NGO, NFE, or thin custom host loop | `[EXPERIMENT REQUIRED]` |
| First local slice | **In-process “local host” command bus** (no package) | **S2-01** |
| First networked slice | Smallest listen-server or loopback host+client on **same** PhysicsTest drives | **S2-02 PASS** (in-process loopback; no package) |

**Rationale:** EXP-07 (input sync) and EXP-06 (authority) can be partially proven locally before paying package/complexity cost. EXP-05 (cross-process sync) needs a real transport — chosen only after S2-01 proves the command path.

## 4. Spike sequence

| ID | Hypothesis | In scope | Out of scope | Maps to |
|----|------------|----------|--------------|---------|
| **S2-01** | Two robots can be driven only via a shared host-like command bus (same `PhysicsTestDriveCommand` → `PhysicsTestDrive`) | Dual input → bus → two drives; local “cheat” overwrite smoke; verifier | Real sockets, NGO, prediction, dedicated server | EXP-06/07 prep, ARCH C1 thin |
| **S2-02** | Two processes/players see consistent motion with provisional transport | Listen-server or loopback; command RPC; host sim | Full prediction, matchmaking, damage | EXP-05 thin, EXP-07 |
| **S2-03** | Illegal client force/state does not win | Cheat impulse/velocity/teleport smoke vs host reconcile | Full anti-cheat | EXP-06 |
| **S2-04** | Feel under latency harness | RTT/jitter/loss sim; decide interpolate/predict need | Production netcode freeze | EXP-08 |
| **S2-05** | Dedicated/headless smoke | Thin server build tick | Scale | EXP-09 thin |

## 5. S2-01 success / fail (this session’s first slice)

### Success

- Robot_A and Robot_B both move under **bus-submitted** commands (not device→drive direct).
- Same drive forces as S1-02; PhysicsTest arena/robots reused (A keeps S1-03 hinge if built from that base).
- Verifier: no NaN; both robots show readable displacement; optional cheat `SetCommand` is overwritten by authority same FixedUpdate.
- **No new packages.**

### Fail

- Must bypass bus to get stable dual drive.
- Authority path diverges into a second physics model.
- Requires package for this local question.

## 6. Explicitly NOT in S2-01

- NGO / Unity Transport / NFE install  
- Lobby, relay, dedicated server  
- Prediction / interpolation  
- Blueprint spawn / modular factory (ARCH H note deferred; documented risk)  
- Damage / detach  
- Freezing net stack in decision log as “accepted”

## 7. Decision log updates (expected)

After S2-01: provisional note that **command→authority→drive** works locally on PhysicsTest.  
Networking solution remains **Open** until S2-02+.

---

*Spike sequence S2-01…S2-05 complete. Gate: [STAGE2_GO_NOGO.md](./STAGE2_GO_NOGO.md). Next: Stage 3 modular architecture thin slice + Stage 2 closure (cross-process / dedicated build).*
