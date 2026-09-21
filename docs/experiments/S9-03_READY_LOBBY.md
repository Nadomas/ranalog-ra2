# S9-03 — Ready / Lobby Flow Stub

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S9-03 Ready Lobby)`

## Goal

Prove two LAN/listen-host roles can **ready → start** without manual scene hacking. Thin UX + host-owned start gate; fight/admit spawn remain S9-01.

## Code

| Piece | Role |
|-------|------|
| `MatchReadyLobbyFlow` | Plain C# ready gate → `TryStart` (Lobby → Admitting) |
| `MatchLobbySession.IsPeerReady` | Seat ready query for chrome |
| `RobotReadyLobbyChrome` | IMGUI: Ready Host / Ready Client / Start Match |
| `RobotReadyLobbyVerifier` | In-process UDP host+client on port **7793** |

## Acceptance

- Peer link required before start (`peers_not_linked`)
- Both seats ready required (`peers_not_ready`)
- After both ready → Start → `MatchPhase.Admitting`

## Pass log

```
[S9-03] VERIFIER_DONE pass=True reason=ok phase=Admitting session=s9-ready-stub started=True all_ready=True seat0=True seat1=True peer_host=True peer_client=True early_blocked=True status=started phase=Admitting
```

## Not in this spike

- Loadout select / robot preview hide  
- Matchmaking queue  
- Full fight after start (covered by S9-01)  
- Production lobby UI polish

## Next

Optional weapon hit thin (S07-T02) if GDD feel needs it; otherwise Stage 9–11 thin residuals complete.
