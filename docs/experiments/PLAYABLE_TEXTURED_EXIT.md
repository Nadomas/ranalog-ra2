# PLAYABLE TEXTURED EXIT

> **Status:** **PASS** (2026-09-22)  
> **Plan:** [`docs/ai/PLAYABLE_TEXTURED_PLAN.md`](../ai/PLAYABLE_TEXTURED_PLAN.md)  
> **Player:** `UnityProject/ra2-analog/Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe`  
> **Rebuild:** 2026-09-22 — `PLAYER_BUILD result=Succeeded` (Editor.log)

## Bar met

1. Design → Wire → Test → Fight → Results on MP-capable path (Stage 11 + S12 soak).  
2. Arena + robot use URP Lit materials with albedo (S12-03…05) — not flat untextured greys.  
3. Timeout / stalemate readable (`TimeExpired` + results title).  
4. Smoke green including soak / stalemate / textured probes.

## Evidence

| ID | Doc |
|----|-----|
| S12-T01 | [`S12-01_SOAK.md`](S12-01_SOAK.md) |
| S12-T02 | [`S12-02_STALEMATE_UX.md`](S12-02_STALEMATE_UX.md) |
| S12-T03 | [`S12-03_MATERIAL_KIT.md`](S12-03_MATERIAL_KIT.md) |
| S12-T04 | [`S12-04_ARENA_TEXTURES.md`](S12-04_ARENA_TEXTURES.md) |
| S12-T05 | [`S12-05_ROBOT_TEXTURES.md`](S12-05_ROBOT_TEXTURES.md) |

## Smoke contract

```
SMOKE_DONE pass=True … arena=True textured=True robotTex=True soak=True stalemate=True
```

Soak menu: `Tools/RA2/Play MVP Soak (S12-01)`.

## Still deferred

Paint shop, full parts catalog, career/economy, detach debris, dedicated soak farm, NGO freeze.
