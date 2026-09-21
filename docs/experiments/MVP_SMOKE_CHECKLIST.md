# MVP Smoke Checklist — Thin touchable loop

> Living checklist toward “пощупать” GDD MVP.  
> Updated: 2026-09-21 — **Stage 11 EXIT PASS** ([`STAGE11_EXIT.md`](STAGE11_EXIT.md)).

## Blessed player path

| Step | How | Expect |
|------|-----|--------|
| Build | `Tools/RA2/Build MVP Windows Player (S11-07)` | `Builds/Ra2MvpPlayer/Ra2MvpPlayer.exe` |
| Auto smoke | `Tools/RA2/Play MVP Smoke (S11)` or `-ra2-mvp-smoke` | `SMOKE_DONE pass=True` all flags |
| Manual | Run `.exe` | Design → Wire → Drive → Admit → Local/UDP/LAN → Results → History |

## Smoke flags (current)

`design cfg test inst admit wire grid save debug local udp lan history arena`

## Manual touch path

1. Design: nudge poly; **Save Bot** / **Load Bot**.  
2. Wire: controller Kind/Binding; Sign/Channel; TankSteer.  
3. Drive: WASD; watch **CONTROL DEBUG**.  
4. Fight: Local / UDP Loopback / Host–Join LAN (`7796`).  
5. Results overlay + **History**.

## Known issues / residuals (non-blockers)

- Freehand polygon gizmo / full composite binding matrix  
- Full GDD drag-wire canvas (grid is thin)  
- Dedicated Server Win module not installed (headless residual)  
- Custom UDP not package-frozen; NGO/NFE unproven  
- Detach debris net; weapon catalogue  
- Reconnect / ranked / spectator / replay scrubber  
- No economy / catalog / career  

## Critical desync class

None open for thin path: host publishes outcomes; clients display only.
