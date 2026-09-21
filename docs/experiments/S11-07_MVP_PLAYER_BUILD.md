# S11-07 — First playable Windows MVP player build

> **Status:** **PASS** (2026-09-21) — Editor Play + Standalone Windows64 smoke  
> **Menus:**  
> - `Tools/RA2/Build MvpPlayable Scene (S11-07)`  
> - `Tools/RA2/Build MVP Windows Player (S11-07)`  
> **Output:** `UnityProject/ra2-analog/Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe` (gitignored `Builds/`)

## Goal

Ship a **Standalone Windows64** player that boots a playable loop without Unity Editor menus: Design/Configure/Test workshop chrome, WASD Test drive, Local Fight 1v1 → results persist.

## Code

| Piece | Role |
|-------|------|
| `RobotMvpPlayableApp` | Runtime world + workshop + fight + `-ra2-mvp-smoke` |
| `RobotMvpPlayableVerifier` | Editor Play Ready smoke |
| `MvpPlayerBuild` | Scene + `BuildPipeline` Windows player |
| Scene `Assets/Scenes/MvpPlayable.unity` | Blessed boot scene |

## How to play

1. Unity: `Tools/RA2/Build MVP Windows Player (S11-07)`.  
2. Run `UnityProject/ra2-analog/Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe`.  
3. Design → Nudge / Configure → Cycle / Test → WASD / Prepare Admit / **Local Fight 1v1**.

## Smoke

```text
Ra2MvpPlayer.exe -ra2-mvp-smoke -batchmode -nographics
```

Marker: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\ra2-analog\ra2-mvp-smoke.txt`

## Pass log

Editor Play:

```
[S11-07] PLAYABLE_READY
[S11-07] VERIFIER_DONE pass=True ready=True design=True test=True inst=True
```

Player build:

```
[S11-07] PLAYER_BUILD result=Succeeded time=00:00:27 size≈206579463 path=.../Ra2MvpPlayer.exe
```

Standalone smoke (`exit=0`):

```
pass=True
status=done Immobilized winner=1
unity=6000.5.9f1
```

## Not in this spike

- Product UI Toolkit chrome  
- UDP MP inside the player shell (local fight only; MP remains UDP verifiers)  
- Installer / Steam
