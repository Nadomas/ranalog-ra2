# S13-01 — Immobility countdown HUD

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** S8-01 immobility evaluator · S12-02 fight pill  
> **Presentation-only** — does not change win authority

## Goal

Make the GDD primary win path readable mid-fight: when a seat accrues immobility, show remaining seconds on YOU/AI labels and a `LOCK …` pill.

## Implementation

| Piece | Role |
|-------|------|
| `ImmobilityWinEvaluator.FormatSideHud` / `FormatLockPill` | Pure presentation helpers |
| `RobotMvpPlayableApp.PushImmobilityHud` | Updates labels each fight tick |
| Local / UDP / LAN host loops | Call push after `rules.Tick` |
| `RobotMvpUiShell` fight-pill | Prefers lock pill over TIME/LIVE |

## Pass log

```
[S13-01] IMMOBILITY_HUD_SMOKE pass=True side=YOU · 0.6 pill=LOCK YOU 0.6
[S11-07] SMOKE_DONE … immobHud=True
```

## Out of scope

Full GDD countdown chrome art, audio cue, spectator overlay.
