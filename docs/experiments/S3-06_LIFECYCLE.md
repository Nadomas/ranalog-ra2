# S3-06 — Modular Lifecycle (disable / detach / despawn)

> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S3-06 Lifecycle)`

## Hypothesis

Assembled robots share a lifecycle: create → drive → **disable** (no further drive force) → **detach** hinged part (drop joint, unparent, keep dynamic RB) → despawn. No transform teleports.

## In scope

- `PhysicsTestDisableFlag` on spawned modular bots
- `RobotSpawnService.TryDetach` for `wheel_fl`
- Despawn clears instance

## Out of scope

- Full damage / fracture formula
- Net replication of detach debris
- Catalog of breakable parts

## Pass log

```
[S3-06] VERIFIER_DONE pass=True moved_enabled=True still_while_disabled=True disable_flag=True detach_ok=True unparented=True has_rb=True no_hinge=True chassis_alive=True destroyed=True
```

**Date:** 2026-08-24 · Play 1× PASS (after adding DisableFlag before Drive so Awake binds the flag).
