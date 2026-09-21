# S3-00 — STAGE 3 Spike Plan (Modular Robot Architecture)

> **Date:** 2026-08-21  
> **Status:** S3-01..S3-07 **PASS** (thin); see [STAGE3_EXIT.md](./STAGE3_EXIT.md) (**PARTIAL** exit)  
> **Depends on:** [STAGE2_GO_NOGO.md](./STAGE2_GO_NOGO.md) **PROVISIONAL GO**, [TECHNICAL_ROADMAP.md](../TECHNICAL_ROADMAP.md) STAGE 3  
> **Caution:** ARCH C2 — sync surface may change; keep assembly output compatible with Stage 2 command→authority→drive. Stage 2 HARD GATE still not fully closed.

---

## 1. Goal

Replace hand-built PhysicsTest robot hierarchy with **data-driven assembly** that produces the same physical drive surface (`PhysicsTestDrive` / Rigidbody) so Stage 2 net path can spawn from blueprints.

## 2. Spike sequence (thin)

| ID | Hypothesis | Status | In scope | Out of scope |
|----|------------|--------|----------|--------------|
| **S3-01** | Chassis+wheels assemble from blueprint data and drive via existing authority path | **PASS** | Plain C# defs + assembler; one sample blueprint; local Play verify | Editor UX, catalog, JSON freeze, net spawn |
| **S3-02** | Blueprint serialize round-trip (U-SER thin) | **PASS** | Save/load sample | Full schema versioning |
| **S3-03** | Host spawns assembled robot for loopback client | **PASS** | Net spawn/despawn event thin; shared `RobotSpawnService` | Damage/detach lifecycle; cross-process |
| **S3-04** | RA2-aligned blueprint v1 (chassis/wiring/power) | **PASS** | `ra2.robot_blueprint.v1` + validator + wiring smoke | Per-motor actuation; UI |
| **S3-05** | Host loopback spawn of v1 + reject invalid | **PASS** | Valid tank v1 admit; no Control Board reject | Cross-process |
| **S3-06** | Disable / detach / despawn on modular robot | **PASS** | Flag + hinge drop + unparent | Net debris / damage formula |
| **S3-07** | Per-motor hinge motors driven by wiring (no root drive) | **PASS** | `RobotMotorDrive` + hinge motors; suppress chassis AddForce | Channel-direction correctness; friction tuning |

## 3. S3-01 success

- `RobotBlueprint` + `RobotAssembler` build Robot_A-like body without hand hierarchy in scene builder.  
- Assembled robot accepts `PhysicsTestDriveCommand` (same drive component).  
- Verifier: no NaN; measurable displacement under auto command.  
- No new packages.

## 4. S3-02 success

- JSON file round-trip (`ra2.robot_blueprint.v0`) restores critical fields (0 mismatches).  
- Deserialized blueprint assembles and drives (same smoke as S3-01 thin).  
- Format provisional — not frozen. See [S3-02_BLUEPRINT_SERIALIZE.md](./S3-02_BLUEPRINT_SERIALIZE.md).

## 5. S3-03 success

- Empty arena; client admits blueprints over loopback; host validates + assembles via shared spawn path.  
- Client receives spawn events with matching logical component graph.  
- Drive + thin poses after spawn; despawn coordinated.  
- See [S3-03_NET_SPAWN.md](./S3-03_NET_SPAWN.md).

## 6. Explicitly NOT in Stage 3 thin

- Full component catalog / economy hooks  
- Net replication of detach debris / full damage formula  
- Freezing serialization format  
- NGO / cross-process transport  
- Construction UX (Stage 4)

---

*Exit record: [STAGE3_EXIT.md](./STAGE3_EXIT.md). Next: Stage 2 HARD GATE closure (cross-process) and/or Stage 4/5 thin spikes on this runtime.*
