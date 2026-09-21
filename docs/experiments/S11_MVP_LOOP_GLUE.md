# S11 — MVP Loop Glue (Workshop → Combat Admit)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S11 MVP Loop Glue)`

## Goal

One coherent path: Design → Configure → Test (workshop) → prepare combat admit → **local** fight+results **and** one **UDP MP** admit fight+results. Same blueprint schema; no divergent battle format.

## Code

| Piece | Role |
|-------|------|
| `RobotWorkshopSession.TryPrepareCombatAdmit` | Leave Test, validate, JSON clone for admit |
| `RobotMvpLoopVerifier` | Workshop → local Immobility fight → UDP lobby admit fight |
| Reuses | `MatchLobbySession`, `RobotHostSpawnerUdp`, `MatchResultsStub`, damage/win stack |

## Pass log

```
[S10-01] RESULTS side=local session=s11-local finished=True reason=Immobilized winner=1 loser=0 ...
[S10-01] RESULTS side=mp-host session=s11-mp finished=True reason=Immobilized winner=1 loser=0 ...
[S10-01] RESULTS side=mp-client session=s11-mp finished=True reason=Immobilized winner=1 loser=0 ...
[S11] VERIFIER_DONE pass=True local_ok=True mp_ok=True local_reason=Immobilized mp_winner=1 mp_loser=0 client_reason=Immobilized json_len=7089 workshop_switches=2
```

## Not in this spike

- Full GDD MVP chrome (construction UI, binding UI, arena art)  
- Economy / catalog content  
- Disconnect policy soak / production MM  
- Multi-mode playlists

## Residuals toward product MVP

- Construction / Configure UI (local-only)  
- Weapons / damage formulae beyond functional disable  
- Disconnect policy + dedicated server path polish  
- Results UI beyond console stub + local summary persist  
- Smoke checklist / blessed content freeze doc
