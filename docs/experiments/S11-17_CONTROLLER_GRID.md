# S11-17 — Fuller controller-grid wiring chrome

> **Status:** **PASS** (2026-09-21 smoke)  
> **Depends on:** S11-13 wiring canvas  
> **UI:** Configure — CONTROLS grid + WIRES BY CONTROL

## Goal

Thin GDD controller surface: each control slot shows **Kind** (Switch/Button/Analog) and **InputBinding**; wires listed under their control; conflict count visible. Still not freehand wire editor.

## Code

| Piece | Role |
|-------|------|
| `TryCycleSlotKind` / `TryCycleSlotBinding` | Pure slot edits |
| `RobotMvpUiShell.RebuildControlGrid` | Slot rows |
| Grouped `RebuildWireCanvas` | Wires under slot headers |
| `wire-conflicts` | Duplicate / group overlap count |

## Pass log

```
[S11-17] CONTROLLER_GRID_SMOKE pass=True kind=Analog->Analog bind_cycled=True slots=2
[S11-07] SMOKE_DONE pass=True … wire=True grid=True …
```

## Out of scope

Drag-wire canvas, composite action editor, full controller palette catalog.
