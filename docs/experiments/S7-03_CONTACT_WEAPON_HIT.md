# S7-03 — Contact / Collision Weapon Hit Thin

> **Status:** **PASS** (2026-09-21 Play)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S7-03 Contact Weapon Hit)`  
> **Covers:** Stage 7 weapons depth beyond manual S7-02 apply — collision relative speed → concussion/piercing → host damage

## Goal

Prove contact-driven weapon hits using the existing S7-02 host apply path: relative collision speed maps to impact, then degrade/disable. No weapons catalog, no Transform teleport on dynamics, host-only outcomes.

## Formula (thin)

```
impact = clamp(relativeSpeed × 0.15, 0..2.5)   // 0 if speed < 0.75
severity = impact × mean(concussion, piercing) × (1 − armor)   // via RobotDamageService
```

## Code

| Piece | Role |
|-------|------|
| `RobotContactWeaponHit` | Plain C# impact-from-speed + `TryApplyFromContact` |
| `RobotContactWeaponProbe` | MB OnCollisionEnter → host apply (cooldown) |
| `RobotInstanceTag` | Spawn-time root tag so probes resolve victims |
| `RobotSpawnService.Spawn` | Binds `RobotInstanceTag` |
| `RobotContactWeaponHitVerifier` | Soft no-op + unit degrade + physics head-on slam |

## Authority

- Probe gated by `HostAuthority` (default true).
- Clients must not decide degrade/disable; they may present replicated outcomes later.
- Forces/impulses + collision only — no `transform` writes on dynamic bodies.

## Pass log

```
[S7-03] VERIFIER_DONE pass=True soft=True unit_degrade=True physics=True hits=1 last_speed=28.42 last_impact=2.50 last_out=Disabled victim_scale=0.00 mid_impact=0.98 mid_sev=0.63
```

## Not in this spike

- Spinner/flipper Fire channel motors  
- BurstPiston air impulse  
- Chassis splash / armor tables / catalog art  
- Net replication of contact hit events beyond existing combat UDP

## Next

S6-02 Test Room reset UX; S10-03 results readable; optional BurstPiston Fire thin.
