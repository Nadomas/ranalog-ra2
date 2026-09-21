# S6-04 — Workshop Admit → Test clone

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S6-04 Workshop Admit→Test)`

## Goal

After **Prepare Admit**, return to Test Room by spawning the **combat-admit JSON clone** (not only Reset of working Design). Reject Test Admit before Prepare.

## Code

| Piece | Role |
|-------|------|
| `RobotWorkshopSession.TryEnterTestFromAdmit` | Clone LastAdmitJson → validate → Test spawn |
| `RobotWorkshopChrome` | **Test Admit Clone** button |
| `RobotWorkshopAdmitTestVerifier` | reject → Design/Configure/Test → Admit → Test Admit → drive |

## Pass log

```
[S6-04] VERIFIER_DONE pass=True reject_early=True/no_admit design=True cfg=True test=True admit=True/ left_test=True admit_test=True/ mode_test=True new_inst=True drove=True ms=2,3 status=admit_test ms=2,3
```

## Authority / local-only

- Chrome remains local-only Test Room UX.
- Admit clone uses the same serializer path as MP spawn.

## Not in this spike

- Persist admit snapshot across Editor sessions  
- Multi-robot Test Room
