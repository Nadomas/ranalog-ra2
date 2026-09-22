# S14-01 — Starter part textures (spin / battery / board)

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** S12-03 material kit · S12-05 robot textures  
> **Presentation-only** — does not change physics materials or admit rules

## Goal

Make starter powertrain parts visually distinct: battery (stripes), spin motor (noise blue), control board (green checker) via the shared URP Lit kit.

## Implementation

| Piece | Role |
|-------|------|
| `MvpMaterialKitBuilder` | Generates `tex_battery` / `tex_spin` + `MvpBattery` / `MvpSpin` mats |
| `RobotMvpMaterialKit.Battery` / `.Spin` / `.Board` | Resources load or runtime fallback |
| `RobotAssembler` | Applies Battery / Spin / Board mats by `RobotComponentBase` |
| Smoke | `TrySmokeStarterPartMats` — mat names present |

## Pass log

```
[S14-01] MATERIAL_KIT_BUILT … (+battery/spin)
[S14-01] STARTER_PART_MATS_SMOKE pass=True battery=MvpBattery spin=MvpSpin board=MvpBoard
[S11-07] SMOKE_DONE … starterMats=True
```

## Out of scope

Paint shop, per-team liveries, hand-painted atlas beyond thin procedural albedos.
