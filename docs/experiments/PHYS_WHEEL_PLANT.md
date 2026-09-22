# PHYS — Wheel plant / gravity after impacts

> **Status:** **PASS** (2026-09-22)  
> **Symptom:** After hits, bots tipped/lifted and wheels stayed airborne (vacuum feel).

## Root cause

1. Chassis `RigidbodyConstraints.FreezeRotationX | FreezeRotationZ` blocked pitch/roll recovery.  
2. `ApplyToRootBody` set root mass = **sum of all parts** while wheels kept their own RBs → double mass + bad CoM.

## Fix

- Remove freeze; keep gravity on; modest angular damping.  
- Root mass/CoM only from chassis + non-RB parts; bias CoM slightly down.  
- Explicit `useGravity` on dynamic part bodies.

## Smoke

```
[PHYS] PLANT_SMOKE pass=True setup=True planted=True … g=True constraints=None
[S11-07] SMOKE_DONE … phys=True
```
