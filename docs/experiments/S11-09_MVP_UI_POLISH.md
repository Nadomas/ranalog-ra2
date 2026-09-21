# S11-09 — MVP workshop UI polish

> **Status:** **PASS** (2026-09-21 Play)  
> **Depends on:** S11-08 UI Toolkit shell  
> **Rebuild player:** `Tools/RA2/Build MVP Windows Player (S11-07)` (picks up UXML/USS)

## Goal

Make the product UI Toolkit shell feel like a real workshop HUD (not a raw control dump): flow steps, admit badge, contextual help, clearer match pipeline, results card.

## Changes

| Piece | Role |
|-------|------|
| `MvpWorkshop.uxml` | Flow header, help copy, key chips, bottom bar, admit badge |
| `MvpWorkshop.uss` | Stronger industrial hierarchy / states |
| `RobotMvpUiShell` | Drives flow / badge / fight pill / help line |
| `RobotMvpUiVerifier` | Asserts flow + admit-badge + bottom-bar |

## Pass log

```
[S11-09] VERIFIER_DONE pass=True ui=True root=True design=True cfg=True test=True inst=True imgui_off=True
```

## Not in this spike

- Full wiring canvas / controller grid  
- UDP lobby screens  
- New fonts / custom sprites
