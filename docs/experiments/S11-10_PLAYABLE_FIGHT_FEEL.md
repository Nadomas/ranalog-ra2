# S11-10 — Playable local fight feel (drive + chase AI)

> **Status:** **PASS** (2026-09-21)  
> **Depends on:** S11-07/09 player shell

## Goal

Make Local Fight feel like a short duel instead of the smoke stub:
- player keeps WASD control (no auto-disable after 0.4s)
- opponent uses thin chase AI (face-then-advance, arena soft leash)
- fix inverted tank wiring signs for hinge CW
- less “ice / no gravity” skate (chassis grip + gravity explicit)
- arena walls + out-of-bounds forfeit

Smoke (`-ra2-mvp-smoke`) keeps the fast disable→immobility path for CI.

## Code

| Piece | Role |
|-------|------|
| `RobotSimpleChaseAi` | Seek ControlState |
| `RobotMvpPlayableApp` | Interactive vs smoke fight paths |
| `RobotControlConfigurer` TankSteer | forward/turn Sign -1 |
| `RobotAssembler` | chassis grip + gravity damping |

## Pass

Editor UI verifier + player rebuild; smoke still exits 0.
