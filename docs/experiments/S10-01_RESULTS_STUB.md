# S10-01 — Results Stub from MatchOutcome

> **Status:** **PASS** (2026-09-21 Play, verified with S9-01)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S10-01 Results Stub)` (= S9-01 scene)  
> **Contract:** console `MatchResultsStub.Present` — not a polished UI screen

## Goal

After authoritative fight end, both sides show/log the same facts: win reason, winner/loser, immobile seconds, duration, loser-disabled flag.

## Code

| Piece | Role |
|-------|------|
| `MatchSummary` | Authoritative summary schema |
| `MatchResultsStub.Format` / `Present` | Local-only presentation (Debug.Log contract) |

## Pass log

```
[S10-01] RESULTS side=host session=s9-udp-thin finished=True reason=Immobilized winner=1 loser=0 duration_s=1.76 immobile_loser_s=1.22 immobile_winner_s=1.22 loser_disabled=True
[S10-01] RESULTS side=client session=s9-udp-thin finished=True reason=Immobilized winner=1 loser=0 duration_s=1.76 immobile_loser_s=1.22 immobile_winner_s=1.22 loser_disabled=True
```

Host and client payloads agree on reason/winner/loser/session (clients do not recompute winners).

## Not in this spike

- Results UI chrome / history list  
- SummaryStore persistence backend  
- Full EventLog → StatsAggregator telemetry product

## Next

S11 glue workshop → combat admit paths.
