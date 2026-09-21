# S9-04 — Heartbeat Disconnect (no goodbye)

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S9-04 Heartbeat Disconnect)`  
> **Policy tags:** `disconnect-forfeit-v0` + `heartbeat-timeout-v0`

## Goal

Prove mid-fight **silent peer death** (socket hard-close, no `MsgGoodbye`) still ends the match via host heartbeat timeout → same `DisconnectForfeit` as S9-02. No silent desync; disconnect remains a loss for the dropped seat.

## Policy

| When | Host action |
|------|-------------|
| Phase = Fighting | `ArmHeartbeatWatch()` then client `AbortTransport()` (no goodbye) |
| Silence ≥ heartbeat timeout while peer was ready | `PeerLostByHeartbeat` → `PeerReady=false` |
| Then | `MatchDisconnectPolicy.TryResolveForfeit` → `MatchWinReason.DisconnectForfeit` |

## Code

| Piece | Role |
|-------|------|
| `MatchPeerHeartbeat` | Pure silence clock |
| `PhysicsTestUdpCodec.MsgHeartbeat` | Keepalive while peer alive |
| `PhysicsTestUdpTransport.ConfigureHeartbeat` / `ArmHeartbeatWatch` / `AbortTransport` | Enable timeout after fight; crash-style close |
| `RobotHeartbeatDisconnectVerifier` | Fight → abort → heartbeat → forfeit |

## Pass log

```
[S9-04] VERIFIER_DONE pass=True reason=ok policy=disconnect-forfeit-v0 heartbeat=heartbeat-timeout-v0 phase=Closed session=s9-heartbeat-v0 host_pub_outcome=1 winner=0 loser=1 host_reason=DisconnectForfeit peer_host=False lost_by_hb=True hb_timeouts=1
```

## Not in this spike

- Reconnect window / bot replace  
- Adaptive RTT-based timeouts  
- Dedicated soak
