# S11 — Workshop Construction/Configure Chrome (thin)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S11 Workshop Chrome)`

## Goal

Touchable local-only chrome for Design ↔ Configure ↔ Test on the existing workshop session — usable, not polished. Same blueprint path as combat admit (`TryPrepareCombatAdmit`).

## Code

| Piece | Role |
|-------|------|
| `RobotWorkshopChrome` | IMGUI: mode buttons, TankSteer preset, Prepare Admit |
| `RobotWorkshopChromeVerifier` | Programmatic Design→Configure→preset→Test→admit |
| Reuses | `RobotWorkshopSession`, `RobotControlConfigurer`, sample A blueprint |

## Pass log

```
[S11-CHROME] VERIFIER_DONE pass=True design=True/ cfg=True/ preset=True/ test=True/ inst=True admit=True/ switches=2 json_len=7087 status=admit_ready json_len=7087
```

## Not in this spike

- Polygon construction editor  
- Binding groups UX / composite UI  
- Arena art / HUD polish

## Notes

Marked **local-only** chrome. Blueprint remains MP-admit compatible (JSON clone via existing serializer).
