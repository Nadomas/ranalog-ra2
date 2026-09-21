# S9-01 — UDP Match Lobby → Admit → Fight → MatchOutcome

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S9-01 Match UDP Lobby)`  
> **Also covers thin S10-01 delivery** (results stub on both sides)

## Goal

Prove match-as-service thin slice: lobby ready → validate/admit blueprints over existing UDP → fight with immobility rules → host emits authoritative `MatchSummary` / `MatchOutcome` to listen-host + remote client.

## Code

| Piece | Role |
|-------|------|
| `MatchLobbySession` | Plain C# Lobby → Admitting → Fighting → Results |
| `MatchSummary` | Outcome + immobile seconds + duration + session id |
| `PhysicsTestUdpCodec.MsgMatchOutcome` | Binary summary payload |
| `PhysicsTestUdpTransport.PublishMatchOutcome` | Host → peer delivery |
| `RobotHostSpawnerUdp` | Reused admit/assemble path |
| `ImmobilityWinEvaluator` + `RobotDamageService` | Authoritative win (disabled preferred as loser) |
| `RobotMatchUdpVerifier` | In-process host+client UDP on port 7791 |

## Pass log

```
[S10-01] RESULTS side=host session=s9-udp-thin finished=True reason=Immobilized winner=1 loser=0 duration_s=1.76 immobile_loser_s=1.22 immobile_winner_s=1.22 loser_disabled=True
[S10-01] RESULTS side=client session=s9-udp-thin finished=True reason=Immobilized winner=1 loser=0 duration_s=1.76 immobile_loser_s=1.22 immobile_winner_s=1.22 loser_disabled=True
[S9-01] VERIFIER_DONE pass=True reason=ok phase=Closed session=s9-udp-thin live=2 admitted=2 host_pub_outcome=1 client_got_outcome=1 winner=1 loser=0 host_reason=Immobilized client_reason=Immobilized same_session=True peer_host=True peer_client=True
```

## Not in this spike

- Production matchmaking / reconnect  
- Dedicated server soak / EXP-09 cost model  
- Disconnect forfeit policy playtest  
- Spectate / full battle UI

## Next

S10 results stub (covered) → S11 workshop→combat glue.
