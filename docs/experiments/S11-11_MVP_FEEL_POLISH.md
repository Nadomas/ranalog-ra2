# S11-11 — MVP feel polish (less raw)

> **Status:** **PASS** (2026-09-21 Play + player rebuild)  
> **Depends on:** S11-10 playable fight feel  
> **Rebuild:** `Tools/RA2/Build MVP Windows Player (S11-07)`

## Goal

Reduce “empty grey slab / tech demo” feel of the Windows player without content gold-plate.

## Changes

| Piece | Role |
|-------|------|
| Boot into **Drive** (Test) on launch | Robot on floor immediately |
| `RobotMvpFollowCamera` | Soft follow in Drive / Fight |
| Arena ring + tinted floor | Readable pit |
| Fight HUD (YOU / timer / AI) | Live duel chrome |
| Side panel copy | Design / Wire / Drive + Start Fight |

## Pass

```
[S11-09] VERIFIER_DONE pass=True ... inst=True
[S11-07] PLAYER_BUILD result=Succeeded
```

Smoke path unchanged (`-ra2-mvp-smoke` skips boot-into-drive).
