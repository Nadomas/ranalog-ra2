# TASK_QUEUE — ra2-analog

> Operational queue for the autonomous coordinator.  
> Derived from [`docs/TECHNICAL_ROADMAP.md`](../TECHNICAL_ROADMAP.md) + evidence in `docs/experiments/` and `UnityProject/ra2-analog/Assets/`.  
> **Rules:** complete in order within the current milestone; do **not** ask the human for the next task while an unfinished open item exists; log real stops in [`BLOCKERS.md`](BLOCKERS.md).

---

## Status

| Field | Value |
|-------|--------|
| **Current milestone** | **STAGE 14** post-textured polish — **PASS** (idle) |
| **Roadmap position** | Stage 14 P2 PASS; plan §5 P3 residual / Later backlog only on explicit pull |
| **Queue status** | **Idle** — S14-T01 / S14-T02 done; PIPE-T02 human-only |
| **Last queue update** | 2026-09-22 |
| **Autonomy recommendation** | Level **2–3** (see [`AI_WORKFLOW.md`](AI_WORKFLOW.md)) |
| **Evidence snapshot** | [`S14-01_STARTER_PART_TEXTURES.md`](../experiments/S14-01_STARTER_PART_TEXTURES.md) · [`S14-02_FIRE_WIRING_UI.md`](../experiments/S14-02_FIRE_WIRING_UI.md) · plan §5 |

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
| S04-T02 | Chassis polygon editor thin (≤16 pts) | done | `S4-02_CHASSIS_POLYGON_EDITOR.md` |

### Stage 5 — Control (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S05-T01 | Configure wiring presets (S5-01) | done | `S5-01_CONFIGURE_WIRING.md` |
| S05-T02 | Binding groups UX thin | done | `S5-02_BINDING_GROUPS.md` |

### Stage 6 — Seamless loop (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S06-T01 | Seamless Design↔Configure↔Test (S6-01) | done | `S6-01_SEAMLESS_LOOP.md` |
| S06-T02 | Test Room reset UX thin | done | `S6-02_TEST_RESET.md` |
| S06-T03 | Additive multi-scene U-SCN polish | deferred | Not required for thin MVP path |
| S06-T04 | Workshop Admit → Test admit clone | done | `S6-04_WORKSHOP_ADMIT_TEST.md` |

### Stage 7 / 8 — Damage + combat spine (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S07-T01 | Functional disable damage service | done | `RobotDamageService`; `S8-01_COMBAT_IMMOBILITY.md` |
| S07-T02 | Weapon hit apply thin (concussion/piercing) | done | `S7-02_WEAPON_HIT.md` |
| S07-T03 | Contact/collision weapon hit thin | done | `S7-03_CONTACT_WEAPON_HIT.md` |
| S07-T04 | Spinner SpinMotor Fire/CW via Button/Switch | done | `S7-04_SPINNER_FIRE.md` |
| S07-T05 | BurstPiston Fire air budget thin | done | `S7-05_BURST_PISTON_FIRE.md` |
| S07-T06 | BurstMotor Fire arc thin | done | `S7-06_BURST_MOTOR_FIRE.md` |
| S07-T07 | ServoMotor Analog slow + lock | done | `S7-07_SERVO_MOTOR_ANALOG.md` |
| S07-T08 | ServoPiston Analog Extend/Retract (air) | done | `S7-08_SERVO_PISTON_ANALOG.md` |
| S07-T09 | SmartZone contact → optional Fire | done | `S7-09_SMARTZONE_FIRE.md` |
| S07-T10 | Steering hub Analog Turn ±35° + lock | done | `S7-10_STEERING_HUB.md` |
| S07-T11 | BurstMotor Fire electric draw + deny | done | `S7-11_BURST_MOTOR_ELECTRIC.md` |
| S07-T12 | Air tank recharge via AirMaxInOutRate | done | `S7-12_AIR_RECHARGE.md` |
| S07-T13 | Electric recharge via ElectricMaxInOutRate | done | `S7-13_ELECTRIC_RECHARGE.md` |
| S07-T14 | Ackermann steer + wheel admit | done | `S7-14_ACKERMANN_STEER.md` |
| S08-T01 | Immobility win evaluator (local 1v1) | done | `ImmobilityWinEvaluator`; `S8-01` PASS |
| S08-T02 | Weapons catalog / arena art | deferred | Post thin MVP spine |

### Stage 9 — Full MP battle (thin session)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S09-T01 | Lobby/session thin UDP → MatchOutcome | done | `S9-01_MATCH_UDP_LOBBY.md` |
| S09-T02 | Disconnect policy v0 | done | `S9-02_DISCONNECT_POLICY.md` |
| S09-T03 | Ready/lobby flow stub (LAN/listen-host) | done | `S9-03_READY_LOBBY.md` |
| S09-T04 | Heartbeat disconnect without goodbye | done | `S9-04_HEARTBEAT_DISCONNECT.md` |

### Stage 10 — Results (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S10-T01 | Results stub from MatchOutcome | done | `S10-01_RESULTS_STUB.md` |
| S10-T02 | Persist match summary locally | done | `S10-02_RESULTS_PERSIST.md` |
| S10-T03 | Results screen readable thin | done | `S10-03_RESULTS_READABLE.md` |

### Stage 11 — MVP glue (thin)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S11-T01 | Glue workshop → local combat admit | done | `S11_MVP_LOOP_GLUE.md` |
| S11-T02 | One blessed MP path same blueprint | done | `S11_MVP_LOOP_GLUE.md` |
| S11-T03 | MVP smoke checklist + known-issues | done | `MVP_SMOKE_CHECKLIST.md` |
| S11-T04 | Workshop Construction/Configure chrome (thin) | done | `S11_WORKSHOP_CHROME.md` |
| S11-T05 | E2E smoke Design→Configure→Test→Fight→Results | done | `S11_E2E_LOOP.md` |
| S11-T06 | Workshop unified Design polygon + Configure bindings | done | `S11_06_WORKSHOP_UNIFIED_CHROME.md` |
| S11-T07 | First Windows MVP player build (playable) | done | `S11-07_MVP_PLAYER_BUILD.md` |
| S11-T08 | UI Toolkit workshop shell (replace IMGUI) | done | `S11-08_MVP_UI_TOOLKIT.md` |
| S11-T09 | Workshop UI polish (flow / HUD / results) | done | `S11-09_MVP_UI_POLISH.md` |
| S11-T10 | Playable local fight feel (drive + chase AI) | done | `S11-10_PLAYABLE_FIGHT_FEEL.md` |
| S11-T11 | MVP feel polish (boot Drive, camera, fight HUD) | done | `S11-11_MVP_FEEL_POLISH.md` |
| S11-T12 | UDP loopback fight in Windows player shell | done | `S11-12_UDP_PLAYER_SHELL.md` |
| S11-T13 | Thin wiring canvas (Configure Sign/Channel) | done | `S11-13_WIRING_CANVAS.md` |
| S11-T14 | LAN Host/Join (2-process) in player shell | done | `S11-14_LAN_HOST_JOIN.md` |
| S11-T15 | Thin match history list (local persist UI) | done | `S11-15_MATCH_HISTORY.md` |
| S11-T16 | Stronger thin arena art (procedural pit) | done | `S11-16_ARENA_ART.md` |
| S11-T17 | Fuller controller-grid wiring chrome | done | `S11-17_CONTROLLER_GRID.md` |
| S11-T18 | Blueprint local save/load (session persistence) | done | `S11-18_BLUEPRINT_SAVE.md` |
| S11-T19 | Control debug visualization in Test | done | `S11-19_CONTROL_DEBUG.md` |
| S11-T20 | Stage 11 MVP exit (checklist + known-issues) | done | `STAGE11_EXIT.md` + `MVP_SMOKE_CHECKLIST.md` |

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

### Stage 14 — Post-textured polish (done)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S14-T01 | Distinct spin/battery/board materials | done | `S14-01_STARTER_PART_TEXTURES.md` |
| S14-T02 | Burst/Fire wiring UI path (thin) | done | `S14-02_FIRE_WIRING_UI.md` |

### Stage 13 — Playable feel (done)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S13-T01 | Immobility countdown numbers in fight HUD | done | `S13-01_IMMOBILITY_COUNTDOWN_HUD.md` |
| S13-T02 | Freehand chassis polygon gizmo (thin) | done | `S13-02_FREEHAND_CHASSIS_GIZMO.md` |

### Stage 12 — Hardening + playable textured (done)

| ID | Task | Status | Evidence |
|----|------|--------|----------|
| S12-T01 | Thin soak / net stability smoke | done | `S12-01_SOAK.md` |
| S12-T02 | Stalemate / timeout UX | done | `S12-02_STALEMATE_UX.md` |
| S12-T03 | URP material kit | done | `S12-03_MATERIAL_KIT.md` |
| S12-T04 | Arena textured pass | done | `S12-04_ARENA_TEXTURES.md` |
| S12-T05 | Robot part textures | done | `S12-05_ROBOT_TEXTURES.md` |
| S12-T06 | Playable textured exit + rebuild | done | `PLAYABLE_TEXTURED_EXIT.md` |

### Plan (next)

1. Idle after Stage 14 P2 PASS — pull plan §5 **P3** (dedicated server residual) or Later only on explicit human request.  
2. No economy/catalog. PIPE-T02 remains human-only.

---

## Blocked

| Task | Reason | See |
|------|--------|-----|
| — | None | [`BLOCKERS.md`](BLOCKERS.md) |

Residual **non-blockers** (do not stop queue): Unity Dedicated Server Win module not installed (headless player used); custom UDP not frozen.

---

## Backlog (do not pull forward early)

- Full freehand wire editor / GDD-complete controller chrome  
- Construction polygon freehand gizmo; full composite binding chrome  
- Content catalog / economy / career  
- NGO/NFE package freeze (only after experiment vs current UDP)  
- True Dedicated Server build target hardening  
- Detach debris net replication; weapon formulae / chassis splash polish  
- Geometric Ackermann toe tables / 4WS  
- Ranked matchmaking, spectator, replay scrubber  
- Reconnect window / bot replace / installer  

---

## How to mark progress

```markdown
| S09-T03 | … | done | `docs/experiments/S9-03_….md` + commit `abc1234` |
```

Then set **Current milestone** / **first open** to the next `open` row.

---

*Queue owner: coordinator agent + human. Roadmap remains the proof order; this file is the executable checklist.*
