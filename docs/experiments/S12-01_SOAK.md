# S12-01 — Soak / net stability smoke

> **Status:** **PASS** (2026-09-22)  
> **Menu:** `Tools/RA2/Play MVP Soak (S12-01)` · CLI `-ra2-mvp-soak`  
> **Presentation-neutral** — authority path unchanged

## Goal

Repeat UDP loopback + LAN same-process fights in one Play Mode run without critical desync.

## Implementation

| Piece | Role |
|-------|------|
| `RobotMvpPlayableApp.soakMode` | CLI / `EditorPrefs Ra2MvpForceSoak` |
| Smoke loop | 3× (UDP fight + LAN smoke) after baseline local/udp/lan |
| Marker | `ra2-mvp-smoke.txt` → `soak_ok=` |

## Pass log

```
[S12-01] SOAK_ROUND i=1/3 udp=True lan=True
[S12-01] SOAK_ROUND i=2/3 udp=True lan=True
[S12-01] SOAK_ROUND i=3/3 udp=True lan=True
[S12-01] SOAK_DONE pass=True ok=3 fail=0
[S11-07] SMOKE_DONE … soak=True …
```

## Out of scope

Dedicated multi-process soak farm, ranked matchmaking, NGO package freeze.
