# S16-02 — Chassis armor cycle

> **Status:** **PASS** (2026-09-22)  
> **Reference:** RA2 Polymer / Aluminum / Titanium / Steel  
> **Data + mass** — thin tradeoff; not full damage table freeze

## Goal

In Design, cycle armor types and scale chassis component mass (density factors vs Aluminum).

## Implementation

| Piece | Role |
|-------|------|
| `RobotChassisArmor` | Density factors + `TryCycleArmor` / CatalogId |
| UXML `btn-armor-cycle` | Design panel |
| Smoke | Aluminum→Titanium mass increases |

## Pass log

```
[S16-02] ARMOR_CYCLE_SMOKE pass=True Aluminum→Titanium mass=12.00→16.20
[S11-07] SMOKE_DONE … armor=True
```

## Out of scope

Per-hit absorb tables, weight-class auto rebracket, paint shop.
