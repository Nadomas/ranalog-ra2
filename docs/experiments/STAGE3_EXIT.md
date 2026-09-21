# STAGE 3 — Exit / GO-NOGO

> **Date:** 2026-08-24  
> **Verdict:** **PARTIAL** (modular runtime + v1 schema + loopback spawn + local lifecycle; not full YES)  
> **Spikes:** S3-01..S3-07 PASS ([S3-00 plan](./S3-00_STAGE3_SPIKE_PLAN.md))  
> **Depends on:** [STAGE2_GO_NOGO.md](./STAGE2_GO_NOGO.md) still **PROVISIONAL GO** (HARD GATE not closed)

---

## 1. What was implemented

| Spike | Result | Evidence |
|-------|--------|----------|
| S3-01 Modular assembly | PASS | Blueprint → `RobotAssembler` → `PhysicsTestDrive`; 3 Play runs |
| S3-02 Serialize (U-SER thin) | PASS | JSON `ra2.robot_blueprint.v0` file round-trip; 0 critical mismatches; 3 runs |
| S3-03 Host net spawn | PASS | Loopback admit → host spawn → graph event → drive/poses → despawn; 3 runs |
| S3-04 Blueprint v1 RA2 | PASS | chassis/armor/power/control/wiring JSON; 0 mismatches after null-string normalize |
| S3-05 Net spawn v1 | PASS | v1 tank admit + reject no Control Board |
| S3-06 Lifecycle | PASS | disable (no drive force) + detach hinge wheel + despawn |
| S3-07 Per-motor hinge motors from wiring | PASS | `RobotMotorDrive` uses hinge motors; tank-steer moves + turns |

Shared spawn path: `RobotSpawnService` used by host net path (and available for local test). No new packages. No Construction UI / economy. No Transform physics cheats.

---

## 2. Criterion map (TECHNICAL_ROADMAP STAGE 3)

### Acceptance Criteria

| Criterion | Result | Notes |
|-----------|--------|-------|
| Robot from data assembles **local** | **PASS** | S3-01 |
| Robot from data assembles in **net spike** | **PASS** (thin) | S3-03 loopback + **S2-09 cross-process UDP** |
| Add sample component = data + small adapter, not rewrite whole game | **PASS** | Kinds + assembler adapters; no catalog |

### Performance Criteria

| Criterion | Result | Notes |
|-----------|--------|-------|
| Assembly time does not break match start / test spawn targets | **PASS** (thin) | `assemble_ms_max≈11` ms; budget gate `<50` ms in verifier |
| Lifecycle events in budget tick | **PASS** (thin local) | S3-06 disable/detach/despawn; no net lifecycle stream |

### Networking Criteria

| Criterion | Result | Notes |
|-----------|--------|-------|
| Spawn/despawn coordinated | **PASS** (thin) | Loopback request/event; client logical graph matches |
| Schema versioning strategy (draft) | **PASS** (draft) | `v0` + `v1`; unknown schema fail-closed; **not frozen** |

### Exit Criteria

| Criterion | Result | Notes |
|-----------|--------|-------|
| Construction and Control can build on **one runtime** | **PASS** (thin) | Shared blueprint + assembler + spawn service + Stage 2 command→authority→drive surface |
| Decision gate: data-driven spawn local+net stable | **PASS** (thin) | Loopback (S3-03) + cross-process UDP (S2-09); package still provisional |

### Deliverables checklist

| Deliverable | Result |
|-------------|--------|
| Modular runtime + sample definitions | **PASS** |
| Blueprint schema v0 | **PASS** (provisional) |
| Blueprint schema v1 (RA2-aligned) | **PASS** (provisional, S3-04) |
| Spawn-in-net demo on modular data | **PASS** (loopback S3-03/S3-05 + cross-process S2-09) |

### Develop list honesty

| Item | Result |
|------|--------|
| Component definition data | **PASS** |
| Connection model | **PASS** |
| Runtime assembly from blueprint | **PASS** |
| Serialization round-trip (U-SER) | **PASS** thin |
| Shared spawn path local + net | **PASS** thin |
| Instance lifecycle create / enable / disable / detach / destroy | **PASS** (local) — S3-06; net debris not replicated |

---

## 3. Implications of Stage 2 HARD GATE GO

Stage 2 HARD GATE is **GO** for non-loopback UDP + modular spawn (see [STAGE2_GO_NOGO.md](./STAGE2_GO_NOGO.md), [S2-09_MODULAR_UDP_SPAWN.md](./S2-09_MODULAR_UDP_SPAWN.md)). Still open / residual:

- Netcode stack freeze (custom UDP provisional — not NGO/NFE freeze)  
- True Dedicated Server Win module (not installed; headless player used)  
- Modular sync surface under larger robots (ARCH C2/C3) may still re-gate  

**STOP** still applies to Construction polish, content catalog, economy, career.

---

## 4. STAGE 3 EXIT

**PASS** (thin) — upgraded from PARTIAL after S2-09 closed cross-process admit.

**Why YES-enough:** Data-driven assembly, provisional schema, shared spawn path, loopback + **cross-process UDP** spawn-in-net demo are enough for Construction/Control **spikes** on one modular runtime.

**Why not production-complete:** Schema not frozen; detach debris not net-replicated; custom UDP still `[EXPERIMENT REQUIRED]`; modular sync surface may still re-gate.

**Recommended stance:** Construction/Control spikes can hang on v1 blueprint + shared spawn. Next product work: Stage 4 thin workshop **or** Stage 5 binding UI — without catalog/economy.

---

## 5. Recommended next step

1. Stage 4 thin Construction validation (chassis ≤16, attachment rules) on v1 blueprint — no catalog.  
2. Optional: UDP spawn of v1 payload (upgrades S3-05 from loopback).

---

*Stage 2 HARD GATE remains PROVISIONAL GO — not closed by this document.*
