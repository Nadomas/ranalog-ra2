# S11-13 — Thin wiring canvas (Configure)

> **Status:** **PASS** (2026-09-21 compile + smoke)  
> **Depends on:** S11-09 UI Toolkit shell, TankSteer preset  
> **UI:** Configure panel `wire-list` rows

## Goal

Expose TankSteer wirings in the workshop Configure UI so players can flip **Sign** and cycle **Channel** (CW↔CCW) without IMGUI chrome.

## Code

| Piece | Role |
|-------|------|
| `RobotControlConfigurer.TryFlipWireSign` / `TryCycleWireChannel` | Pure blueprint edits |
| `RobotWorkshopChrome.TryFlipWireSign` / `TryCycleWireChannel` | Session wrappers |
| `RobotMvpUiShell.RebuildWireCanvas` | ScrollView rows (+1/−1, channel) |
| `MvpWorkshop.uxml` `wire-list` | Product surface (+ Resources copy) |

## Pass log

```
[S11-13] WIRE_CANVAS_SMOKE pass=True …
```

## Out of scope

Full controller grid / freehand wire editor GDD chrome.
