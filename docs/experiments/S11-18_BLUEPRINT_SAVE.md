# S11-18 — Blueprint local save/load

> **Status:** **PASS** (2026-09-21 smoke)  
> **Depends on:** S3-02 serializer, S11-08 UI  
> **UI:** Design — Save Bot / Load Bot

## Goal

Persist the workshop working blueprint to `persistentDataPath/ra2-blueprints/workshop-bot.json` and reload it (MVP SHOULD: robots between sessions). Single local slot — not inventory/career.

## Code

| Piece | Role |
|-------|------|
| `RobotBlueprintStore` | Default path + TrySave/TryLoad |
| `RobotWorkshopChrome.TrySaveBlueprint` / `TryLoadBlueprint` | Session glue |
| UI `btn-bp-save` / `btn-bp-load` | Product entry |

## Pass log

```
[S11-18] BLUEPRINT_SAVE_SMOKE pass=True name=smoke-save-bot wires=8
[S11-07] SMOKE_DONE … save=True …
```

## Out of scope

Multi-slot garage UI, cloud sync, anti-tamper beyond existing validate.
