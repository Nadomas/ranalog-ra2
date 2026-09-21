# S11-19 — Control debug visualization (Test / Fight)

> **Status:** **PASS** (2026-09-21 smoke)  
> **Depends on:** S11-08 UI, PhysicsTestDrive command bus  
> **UI:** Drive panel + arena overlay

## Goal

Local-only live readout of drive command (M/T/F) plus Drive/Turn bindings and slot kinds so wiring can be tested without IMGUI.

## Code

| Piece | Role |
|-------|------|
| `RobotControlDebugFormatter` | Pure format from blueprint + drive |
| `RobotMvpPlayableApp.FormatControlDebug` | Resolves Test/Fight seat |
| `control-debug` / `control-debug-arena` | UI Toolkit labels |

## Pass log

```
[S11-19] CONTROL_DEBUG_SMOKE pass=True text=cmd M=1.00 T=0.25 F=0.50 | …
[S11-07] SMOKE_DONE … debug=True …
```

## Out of scope

Per-wheel effort gizmos, world-space wire lines, net-replicated debug.
