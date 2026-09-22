# S14-02 — Burst/Fire wiring UI path (thin)

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** S5-02 binding groups · S7-04 spinner Fire · S11 Configure chrome  
> **UI Toolkit only** — thin Configure path; no full freehand wire editor

## Goal

In Configure, the player can select the Fire bind group, cycle Fire key presets, and one-click wire Fire → first Spin/Burst actuator.

## Implementation

| Piece | Role |
|-------|------|
| `RobotControlConfigurer.BindingGroupId.Fire` | Group + cycle (`Space`/`F`/`Mouse0`) |
| `EnsureFireSlot` / `TryApplyFireWirePreset` | Button Fire slot + wire to actuator CW/Fire |
| UXML `btn-bind-fire` / `btn-fire-preset` | Fire + Wire Fire buttons |
| `RobotWorkshopChrome.TryApplyFirePreset` | Thin chrome API |
| Smoke | `TrySmokeFireWiring` after Configure enter |

## Pass log

```
[S14-02] FIRE_WIRING_SMOKE pass=True cycle=F bind=F detail=fire→motor_fl/CW
[S11-07] SMOKE_DONE … fire=True
```

## Out of scope

Full freehand wire canvas, multi-actuator Fire fan-out, GDD-complete controller chrome.
