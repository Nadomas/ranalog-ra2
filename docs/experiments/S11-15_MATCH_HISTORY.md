# S11-15 — Thin match history list (local)

> **Status:** **PASS** (2026-09-21 smoke)  
> **Depends on:** S10-02 persist, S11-09 UI Toolkit  
> **UI:** top-bar **History** → overlay list

## Goal

Show the last local saved fights from `persistentDataPath/ra2-match-results/` in the MVP shell. Display-only — no winner recompute, no cloud/career.

## Code

| Piece | Role |
|-------|------|
| `MatchSummaryStore.TryListRecent` | Newest-first JSON load (cap 12) |
| `FormatHistoryLine` / `FormatHistoryDetail` | Readable rows |
| `RobotMvpUiShell` History overlay | Product entry |
| Smoke | `history_ok` after fights persist |

## Pass log

```
[S11-15] HISTORY_LIST_SMOKE pass=True
[S11-07] SMOKE_DONE … history=True
```

## Out of scope

Career ledger, cloud sync, replay scrubber.
