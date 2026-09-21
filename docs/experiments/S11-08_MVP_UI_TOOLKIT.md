# S11-08 — Product UI Toolkit workshop shell

> **Status:** **PASS** (2026-09-21 Play)  
> **Rebuild player:** `Tools/RA2/Build MVP Windows Player (S11-07)` (includes UI)

## Goal

Replace IMGUI chrome in the MVP player with a **UI Toolkit** workshop shell: mode tabs, Design polygon tools, Configure bindings, Test reset, Admit + Local Fight, results overlay.

## Code

| Piece | Role |
|-------|------|
| `Assets/UI/Mvp/MvpWorkshop.uxml` + `.uss` | Layout + industrial theme |
| `MvpPanelSettings.asset` | Runtime panel scaling |
| `RobotMvpUiShell` | Binds buttons → `RobotMvpPlayableApp` APIs |
| `RobotMvpPlayableApp` | UI API + SuppressImgui |
| `RobotMvpUiVerifier` | UI present + mode/nudge/cycle |

## Pass log

```
[S11-08] VERIFIER_DONE pass=True ui=True root=True design=True cfg=True test=True inst=True imgui_off=True
```

## Not in this spike

- Full wiring canvas / controller grid GDD UI  
- UDP lobby screens in player  
- Localization
