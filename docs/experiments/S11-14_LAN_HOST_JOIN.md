# S11-14 — LAN Host / Join (2-process) in player shell

> **Status:** **PASS** (2026-09-21 same-process protocol smoke; 2-exe manual path wired)  
> **Depends on:** S11-12 UDP player shell, S2-09 modular UDP spawn  
> **Port:** `7796` (loopback UDP duel stays on `7795`)

## Goal

True LAN entry in the MVP player: **Host LAN** listens, injects seat 0, waits for peer; **Join LAN** connects to `host:7796`, spawns seat 1, sends WASD as command envelopes. Host is authoritative for sim + `MatchSummary`.

## Code

| Piece | Role |
|-------|------|
| `RobotMvpLanMatchRunner` | Host / Client match coroutines |
| `LanHostLocalSeatDrive` | Host WASD → `PhysicsTestLocalAuthority` seat 0 |
| `PhysicsTestUdpTransport.InjectSpawnRequestLocal` | Host admits seat 0 without client spawn |
| UI `lan-host-field`, `btn-lan-host`, `btn-lan-join` | Product entry |
| Smoke | Same-process Host+Client on `127.0.0.1:7796` |

## How to play (2 processes)

1. Build/run two `Ra2MvpPlayer.exe` (or Editor + player).
2. Both: TankSteer → Admit (optional) → set IP on Join side.
3. Host clicks **Host LAN**; Join clicks **Join LAN** with host IP (`127.0.0.1` for same PC).
4. Host drives seat 0 (WASD); Join drives seat 1 (commands → host).

## Pass log

```
[S11-14] LAN_SMOKE_DONE pass=True winner=…
[S11-07] SMOKE_DONE … wire=True … lan=True
```

## Notes

- Client is presentation/input only for its seat — no local force authority.
- Same-process smoke proves protocol; two-exe is the intended product path.
