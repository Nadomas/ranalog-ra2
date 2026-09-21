# S9-02 — Disconnect Policy v0

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S9-02 Disconnect Policy)`  
> **Policy id:** `disconnect-forfeit-v0`

## Goal

Prove mid-fight peer drop ends the match deterministically (no silent desync). Disconnect is a **loss** for the dropped seat (GDD: abuse must not be rewarded). No reconnect window in this spike.

## Policy

| When | Host action |
|------|-------------|
| Phase ≠ Fighting | Reject forfeit apply |
| Peer goodbye / `PeerReady=false` mid-fight | `MatchDisconnectPolicy.TryResolveForfeit` → `MatchWinReason.DisconnectForfeit` |
| Outcome | Remaining seat wins; disconnected seat loses |
| Delivery | Host publishes `MatchSummary` (dropped peer may not receive — host remains authority) |

## Code

| Piece | Role |
|-------|------|
| `MatchDisconnectPolicy` | Pure forfeit resolution |
| `MatchLobbySession.TryCompleteDisconnectForfeit` | Fighting → Results |
| `MatchWinReason.DisconnectForfeit` | Outcome reason byte |
| `RobotDisconnectPolicyVerifier` | In-process UDP: fight → client StopTransport/goodbye → host forfeit |

## Pass log

```
[S9-02] VERIFIER_DONE pass=True reason=ok policy=disconnect-forfeit-v0 phase=Closed session=s9-disconnect-v0 host_pub_outcome=1 winner=0 loser=1 host_reason=DisconnectForfeit peer_host=False
```

## Not in this spike

- Reconnect window / bot replace  
- Heartbeat timeout without goodbye  
- Ranked grief scoring beyond forfeit loss  
- Dedicated soak (EXP-09)

## Next

S10-02 results persist + thin view; S09-T03 ready/lobby stub optional.
