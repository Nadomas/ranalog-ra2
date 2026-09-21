# TASK_QUEUE — ra2-analog

> Operational queue for the autonomous coordinator.  
> Derived from [`docs/TECHNICAL_ROADMAP.md`](../TECHNICAL_ROADMAP.md) + evidence in `docs/experiments/` and `UnityProject/ra2-analog/Assets/`.  
> **Rules:** complete in order within the current milestone; do **not** ask the human for the next task while an unfinished open item exists; log real stops in [`BLOCKERS.md`](BLOCKERS.md).

---

## Status

| Field | Value |
|-------|--------|
| **Current milestone** | **Stage 9–11 residuals complete** — deferred polish / human pipeline only |
| **Roadmap position** | Stages 0–8 **thin proofs PASS**; Stage 2 HARD GATE **GO**; Stage 3 exit **PASS (thin)**; S9–S11 thin + disconnect/persist/chrome/ready/smoke **PASS**; S07-T02 weapon hit thin **PASS** |
| **Queue status** | **Idle for agents** — no product open tasks; `PIPE-T02` human-only; `PIPE-T03` done (pushed) |
| **Last queue update** | 2026-09-21 |
| **Autonomy recommendation** | Level **2–3** (see [`AI_WORKFLOW.md`](AI_WORKFLOW.md)) |
| **Evidence snapshot** | [`SESSION_2026-09-21_MVP_PROGRESS.md`](../experiments/SESSION_2026-09-21_MVP_PROGRESS.md) (~80–85% thin MVP loop) + [`S7-02_WEAPON_HIT.md`](../experiments/S7-02_WEAPON_HIT.md) |

---

## Operating rules

1. Work the **first open** task (`Status: open`) unless it is blocked — then see Blocked section / `BLOCKERS.md`.
2. Do not skip ahead to content catalog or economy while Stage 9–11 thin residuals remain.
3. Mark tasks `done` only after [`DEFINITION_OF_DONE.md`](DEFINITION_OF_DONE.md).
4. After completing a task: update this file, commit on an allowed branch (when allowed), take the next open task.
5. Spikes that already have PASS reports stay `done` even if product polish remains — polish is a **separate** later task.

---

## Completed (evidence-backed)

### Stage 0 — Research / baseline

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S00-T01 | Unity baseline project + U-VER smoke path | done | `UnityProject/ra2-analog/` @ 6000.5.9f1; `docs/UNITY_ENGINE.md` |
| S00-T02 | Docs spine (Vision/GDD/Roadmap/SDS/context) | done | `docs/*` |
| S00-T03 | Cursor rules + Unity MCP package wired | done | `.cursor/rules/*`, `com.emeryporter.unitymcp`, `.cursor/mcp.json` |

### Stage 1 — Local physics prototype

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S01-T01 | PhysicsTest drive + collision | done | STAGE1_EXIT; PhysicsTest scene/runtime |
| S01-T02 | Wheel/joint spike (S1-03) | done | `S1-03-wheel-joint-spike.md` |
| S01-T03 | EXP-01/02/03 thin reports | done | `EXP-01…03` under `docs/experiments/` |

### Stage 2 — MP physics ★ HARD GATE

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S02-T01 | Local authority command bus (S2-01) | done | `S2-01_LOCAL_AUTHORITY_COMMAND_BUS.md` |
| S02-T02 | Loopback transport (S2-02) | done | `S2-02_LOOPBACK_TRANSPORT.md` |
| S02-T03 | Illegal client force (S2-03) | done | `S2-03_ILLEGAL_CLIENT_FORCE.md` |
| S02-T04 | Latency harness (S2-04) | done | `S2-04_LATENCY_HARNESS.md` |
| S02-T05 | Dedicated tick smoke (S2-05) | done | `S2-05_DEDICATED_TICK_SMOKE.md` |
| S02-T06 | Cross-process UDP (S2-06) | done | `S2-06_CROSS_PROCESS_UDP.md` |
| S02-T07 | Thin combat UDP (S2-07) | done | `S2-07_THIN_COMBAT_UDP.md` |
| S02-T08 | Headless player smoke (S2-08) | done | `S2-08_DEDICATED_PLAYER_SMOKE.md` |
| S02-T09 | Modular UDP spawn (S2-09) | done | `S2-09_MODULAR_UDP_SPAWN.md` |
| S02-T10 | GO/NO-GO writeup | done | `STAGE2_GO_NOGO.md` — **GO** |

### Stage 3 — Modular architecture

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S03-T01 | Modular assembly (S3-01) | done | `S3-01_MODULAR_ASSEMBLY.md` |
| S03-T02 | Blueprint serialize (S3-02) | done | `S3-02_BLUEPRINT_SERIALIZE.md` |
| S03-T03 | Net spawn loopback (S3-03) | done | `S3-03_NET_SPAWN.md` |
| S03-T04 | Blueprint v1 RA2 (S3-04) | done | `S3-04_BLUEPRINT_V1.md` |
| S03-T05 | Net spawn v1 (S3-05) | done | `S3-05_NET_SPAWN_V1.md` |
| S03-T06 | Lifecycle disable/detach local (S3-06) | done | `S3-06_LIFECYCLE.md` |
| S03-T07 | Stage 3 exit | done | `STAGE3_EXIT.md` — PASS (thin) |

### Stage 4 — Construction (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S04-T01 | Construction validation mass/CoM/admit (S4-01) | done | `S4-01_CONSTRUCTION_VALIDATION.md` |
| S04-T02 | Construction editor UX / polygon UI | deferred | Out of thin path; after S09–S11 |

### Stage 5 — Control (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S05-T01 | Configure wiring presets (S5-01) | done | `S5-01_CONFIGURE_WIRING.md` |
| S05-T02 | Binding UI / composite groups UX | deferred | After S09–S11; net re-admit of rebound maps still open |

### Stage 6 — Seamless loop (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S06-T01 | Seamless Design↔Configure↔Test (S6-01) | done | `S6-01_SEAMLESS_LOOP.md` |
| S06-T02 | Additive multi-scene U-SCN polish | deferred | Not required for thin MVP path |

### Stage 7 / 8 — Damage + combat spine (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S07-T01 | Functional disable damage service | done | `RobotDamageService`; `S8-01_COMBAT_IMMOBILITY.md` |
| S07-T02 | Weapon hit apply thin (concussion/piercing) | done | `S7-02_WEAPON_HIT.md` |
| S08-T01 | Immobility win evaluator (local 1v1) | done | `ImmobilityWinEvaluator`; `S8-01` PASS |
| S08-T02 | Weapons catalog / arena art | deferred | Post thin MVP spine |

### Stage 9 — Full MP battle (thin session)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S09-T01 | Lobby/session thin UDP → MatchOutcome | done | `S9-01_MATCH_UDP_LOBBY.md` |
| S09-T02 | Disconnect policy v0 | done | `S9-02_DISCONNECT_POLICY.md` |
| S09-T03 | Ready/lobby flow stub (LAN/listen-host) | done | `S9-03_READY_LOBBY.md` |

### Stage 10 — Results (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S10-T01 | Results stub from MatchOutcome | done | `S10-01_RESULTS_STUB.md` |
| S10-T02 | Persist match summary locally | done | `S10-02_RESULTS_PERSIST.md` |

### Stage 11 — MVP glue (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S11-T01 | Glue workshop → local combat admit | done | `S11_MVP_LOOP_GLUE.md` |
| S11-T02 | One blessed MP path same blueprint | done | `S11_MVP_LOOP_GLUE.md` |
| S11-T03 | MVP smoke checklist + known-issues | done | `MVP_SMOKE_CHECKLIST.md` |
| S11-T04 | Workshop Construction/Configure chrome (thin) | done | `S11_WORKSHOP_CHROME.md` |

### Pipeline meta (this setup)

| ID | Task | Status | Notes |
|----|------|--------|-------|
| PIPE-T01 | docs/ai + autonomous rules + scripts/ai | done | Created with this pipeline setup |
| PIPE-T02 | Cursor Project + Automations (manual UI) | open | Human-only; see AI_WORKFLOW § Manual steps — **not** agent work |
| PIPE-T03 | Commit / push outstanding Unity+experiment work on `agent/*` branch | done | Pushed Stage 2–11 thin MVP path on `agent/ai-pipeline` |

---

## Open (ordered) — current milestone

| ID | Task | Status | Acceptance (thin) |
|----|------|--------|-------------------|
| PIPE-T02 | Cursor Project + Automations (manual UI) | open | Human-only; see AI_WORKFLOW § Manual steps — **not** agent work |
| S04-T02 | Construction polygon editor UI | deferred | See completed table |
| S05-T02 | Binding UI / composite groups UX | deferred | See completed table |

---

## Blocked

| Task | Reason | See |
|------|--------|-----|
| — | None | [`BLOCKERS.md`](BLOCKERS.md) |

Residual **non-blockers** (do not stop queue): Unity Dedicated Server Win module not installed (headless player used); custom UDP not frozen; polygon/binding polish deferred; heartbeat without goodbye still open.

---

## Backlog (do not pull forward early)

- Construction polygon editor UI; Configure binding UI chrome (beyond thin IMGUI)  
- Content catalog / economy / career  
- NGO/NFE package freeze (only after experiment vs current UDP)  
- True Dedicated Server build target hardening  
- Detach debris net replication; weapon formulae / chassis splash polish  
- Ranked matchmaking, spectator, replay  
- Reconnect window / bot replace  

---

## How to mark progress

```markdown
| S09-T03 | … | done | `docs/experiments/S9-03_….md` + commit `abc1234` |
```

Then set **Current milestone** / **first open** to the next `open` row.

---

*Queue owner: coordinator agent + human. Roadmap remains the proof order; this file is the executable checklist.*
