# S7-09 — SmartZone contact → optional Fire

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-09 SmartZone Fire)`

## Goal

Prove a **SmartZone** contact sensor can optionally trigger **BurstMotor Fire** without player Button Fire: wiring `ControlSlotId` names the SmartZone component; foreign-robot contact rising edge → Fire edge on the wired actuator.

## Code

| Piece | Role |
|-------|------|
| `RobotAssembler` | SmartZone trigger volume (+ kinematic RB) |
| `RobotSmartZoneSensor` | OverlapBox foreign contact + rising edge |
| `RobotWiringDriveResolver.ResolveSmartZoneFireTargets` | Zone→Burst* Fire wiring |
| `RobotBlueprintValidator` | Allow SmartZone id as wiring control source |
| `RobotBlueprint.CreateRa2SmartZoneFireSample` | BurstMotor sample + zone wiring |
| `RobotSmartZoneFireVerifier` | Foreign probe force-into-zone; Fire=0 |

## Pass log

```
[S7-09] VERIFIER_DONE pass=True zone_contact=True zone_fired=True fired=True arced=True enters=1 fires=1 zone_fires=1 w=12.355 nan=False
```

## Notes

- Early attempts used `OnTriggerEnter`; compound assembled bodies did not deliver callbacks reliably. Thin proof uses **Physics.OverlapBoxNonAlloc** each FixedUpdate (still physics query, no Transform drive of dynamics).
- Contact identity via `RobotInstanceTag` (probe `BindProbe`).

## Net notes

- Zone Fire is host-side from sim contact + wiring; clients must not claim zone hits.

## Not in this spike

- SmartZone direction / chassis-vs-part discrimination polish
- AI weapon tactics chaining
- Steering hubs (backlog)

## Next

Queue idle aside from human-only `PIPE-T02`; backlog Steering hubs.
