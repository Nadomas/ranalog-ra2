# S11-12 — UDP loopback fight in MVP player shell

> **Status:** **PASS** (2026-09-21 player smoke)  
> **Depends on:** S9-01 UDP lobby + S11-07 player  
> **Rebuild:** `Tools/RA2/Build MVP Windows Player (S11-07)`

## Goal

Ship the blessed **MP path inside the Windows player**: listen-host + loopback client over existing UDP stack, admit workshop blueprint, fight, host publishes `MatchSummary`, client receives same outcome.

## Code

| Piece | Role |
|-------|------|
| `RobotMvpUdpLoopbackRunner` | Host+client UDP match (port 7795) |
| `RobotMvpPlayableApp.TryUiUdpFight` | Workshop → UDP duel |
| UI `btn-udp-fight` | Product entry |
| Smoke | Local fight **and** UDP smoke must pass |

Interactive: WASD on host seat 0, chase AI on seat 1. Smoke: disable→immobility + outcome over UDP.

## Pass log

```
[S11-12] UDP_FIGHT_DONE pass=True reason=ok winner=1 client_winner=1 session=mvp-udp-loop
[S11-07] SMOKE_DONE pass=True ... local=True udp=True
```
