# S12-03 — URP MVP material kit

> **Status:** **PASS** (2026-09-22)  
> **Menu:** `Tools/RA2/Build MVP Material Kit (S12-03)`

## Goal

Thin shared URP Lit materials + albedo under `Resources/Mvp` — no catalog.

## Assets

| Material | Texture | Use |
|----------|---------|-----|
| `MvpFloor` | `tex_floor` checker | Arena pad / floor |
| `MvpApron` | `tex_apron` | Outer apron / stands |
| `MvpHazard` | `tex_hazard` stripes | Ring / lanes / stripes |
| `MvpMetal` | `tex_metal` noise | Fence / tanks / chassis base |
| `MvpRubber` | `tex_rubber` | Wheels |
| `MvpAccent` | `tex_accent` | Posts / motors |
| `MvpBoard` | `tex_board` | Control board / smart zone |
| `MvpWeapon` | `tex_weapon` | Weapon parts |

## Code

- `MvpMaterialKitBuilder` — editor generator  
- `RobotMvpMaterialKit` — runtime load + `ForTeam` tint  

## Pass log

```
[S12-03] MATERIAL_KIT_BUILT mats=Assets/Resources/Mvp/Materials tex=Assets/Resources/Mvp/Textures
```
