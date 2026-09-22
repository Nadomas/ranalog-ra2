# S12-02 — Stalemate / timeout UX

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** Match rules timeout → `MatchWinReason.TimeExpired`

## Goal

Timeout / center-rule outcomes read as stalemate break, not Immobilized.

## Implementation

| Piece | Role |
|-------|------|
| `MatchWinReason.TimeExpired` | Distinct reason enum |
| Local / UDP / LAN runners | Force `TimeExpired` on fight clock expiry |
| `MatchResultsStub.FormatResultsTitle` | `TIME EXPIRED · STALEMATE BREAK` |
| Fight HUD | `TIME N` pill when ≤10s |
| Smoke | `TrySmokeStalemateLabel` |

## Pass log

```
[S12-02] STALEMATE_UX_SMOKE pass=True title=TIME EXPIRED · STALEMATE BREAK
```

## Out of scope

Ranked tiebreak UI, spectator overlays, GDD full results screen art.
