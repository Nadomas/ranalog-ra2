# S15-02 — Rebuild Ra2MvpPlayer after S14/S15 polish

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** S15-T01 · S14 textured kit · PLAYABLE_TEXTURED_EXIT  
> **Presentation/build only** — no physics/net changes

## Goal

Ship a fresh Windows Development player that includes starter part materials, Fire wiring UI, Wire Fire actuator fix, and default weapon spinner workshop bot.

## Evidence

```
[S11-07] PLAYER_BUILD result=Succeeded time=00:00:19.5814381 size=207603340
path=…/Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe
[S11-09] PLAYER_BUILD_WITH_UI_FALLBACK
```

Menu: `Tools/RA2/Build MVP Windows Player (S11-07)`.

Prior Editor smoke (same scripts): `SMOKE_DONE pass=True … fire=True starterMats=True`.

## Out of scope

True Dedicated Server Win subtarget (module not installed); economy/catalog.
