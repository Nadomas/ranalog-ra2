# S15-01 — Default workshop weapon bot

> **Status:** **PASS** (2026-09-22)  
> **Depends on:** S14-02 Fire wiring · S7-04 spinner sample  
> **Local workshop default** — same admit path as combat

## Goal

Day-one Design session starts with a tank + weapon spinner already Fire-wired, so Configure → Wire Fire / Space maps to `spinner_motor`, not a drive axle.

## Implementation

| Piece | Role |
|-------|------|
| `RobotWorkshopChrome.EnsureSession` | `CreateRa2SpinnerFireSample` + TankSteer + `TryApplyFireWirePreset` |
| Smoke | Default BP has `spinner_motor`; Fire cycle + Tank keeps `fire→spinner_motor` |

## Pass log

```
[S15-01] DEFAULT_WEAPON_BOT_SMOKE pass=True name=Ra2SpinnerFireSample cycle=F bind=F detail=fire→spinner_motor/CW keptTarget=spinner_motor
[S11-07] SMOKE_DONE … fire=True
```

## Out of scope

Parts catalog picker, BurstPiston default alternate, Dedicated Server module.
