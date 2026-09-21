# S6-02 — Test Room Reset UX Thin

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S6-02 Test Reset)`  
> **Covers:** Product chrome — Reset Test recreates physics spawn from the same working blueprint

## Goal

Expose and prove Test Room reset: despawn + respawn without leaving Test mode, reject reset outside Test, keep blueprint MP-admit compatible.

## Code

| Piece | Role |
|-------|------|
| `RobotWorkshopSession.TryResetTest` | Already existed (S6-01); timing via `LastResetMs` |
| `RobotWorkshopChrome.TryResetTest` + **Reset Test** button | Local-only IMGUI (enabled only in Test) |
| `RobotTestResetVerifier` | Reject Design → enter Test → drive → reset → new instance → drive |

## Authority / local-only

- Chrome is local-only Test Room UX.
- Blueprint path unchanged — same validator/spawn as combat admit.

## Pass log

```
[S6-02] VERIFIER_DONE pass=True reject_design=True new_inst=True reset_ms=2.4 mode=Test drove=True status_ok=True status=reset ms=2,4
```

## Not in this spike

- Gizmo polish / camera framing  
- Persist Test pose mid-session  
- Multi-robot Test Room

## Next

S10-03 results readable; optional BurstPiston Fire thin.
