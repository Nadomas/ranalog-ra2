# STAGE 11 EXIT — MVP thin playable

> **Status:** **PASS** (2026-09-21)  
> **Player:** `UnityProject/ra2-analog/Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe`  
> **Branch evidence:** `agent/ai-pipeline` (S11-07…S11-19)

## Goal (roadmap)

Assemble Design → Configure → Test → Battle → Results on an MP-capable Windows player without economy/catalog.

## Delivered (thin)

| Area | Evidence |
|------|----------|
| Workshop UI Toolkit | S11-08…09 |
| Local 1v1 + chase AI | S11-10…11 |
| UDP loopback in player | S11-12 |
| Wiring Sign/Channel + controller grid | S11-13, S11-17 |
| LAN Host/Join (2-process) | S11-14 |
| Match history list | S11-15 |
| Arena dressing | S11-16 |
| Blueprint save/load | S11-18 |
| Control debug HUD | S11-19 |
| Smoke automation | `-ra2-mvp-smoke` / `Tools/RA2/Play MVP Smoke (S11)` |

## Latest smoke contract

```
SMOKE_DONE pass=True … wire=True grid=True save=True debug=True
local=True udp=True lan=True history=True arena=True
```

## MVP MUST (thin mapping)

| MUST | Status |
|------|--------|
| Local modular physics robot | PASS (Stages 1–3) |
| Authoritative MP 1v1 path | PASS (Stage 2 GO + S11-12/14) |
| Data-driven blueprint | PASS |
| Construction mass/CoM admit | PASS (S4) |
| Bindings / wiring | PASS thin (S5 + S11-13/17) |
| Seamless Design↔Configure↔Test | PASS (S6) |
| Functional disable + immobility win | PASS (S7/S8) |
| Lobby/session → results | PASS (S9/S10 + player) |
| Integrated MVP build | PASS (S11-07) |

## Known issues (non-blockers)

See [`MVP_SMOKE_CHECKLIST.md`](MVP_SMOKE_CHECKLIST.md) — freehand gizmos, full GDD wire editor, dedicated module, detach debris, catalog/economy remain deferred.

## Critical desync class

None open on thin path: host authority for outcomes; clients display/persist only.

## Next

**Stage 12 Hardening** (thin): soak/net stability smoke, stalemate UX polish — no content drop.
