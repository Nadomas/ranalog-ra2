# S3-02 — Blueprint Serialize Round-Trip (U-SER thin)

> **Status:** PASS (3 Play Mode auto verifier runs)  
> **Date:** 2026-08-21  
> **Plan:** [S3-00_STAGE3_SPIKE_PLAN.md](./S3-00_STAGE3_SPIKE_PLAN.md)  
> **Depends on:** [S3-01_MODULAR_ASSEMBLY.md](./S3-01_MODULAR_ASSEMBLY.md)  
> **Scene menu:** `Tools/RA2/Build PhysicsTest Scene (S3-02 Blueprint Serialize)`

## Hypothesis

PhysicsTest sample blueprints can **JSON serialize → file → deserialize** with zero critical-field loss, then assemble via `RobotAssembler` and drive via `PhysicsTestDrive` without NaN (U-SER thin).

## Method

| Piece | Role |
|-------|------|
| `RobotBlueprintSerializer` | Envelope `ra2.robot_blueprint.v0` via `JsonUtility`; file write/read; `DiffCritical` |
| `RobotBlueprintSerializeVerifier` | Round-trip A/B samples → assemble → short drive smoke |
| Temp cache paths | `Application.temporaryCachePath/ra2_s3_02_sample_{a,b}.json` |

Out of scope: full schema versioning, MessagePack, ScriptableObject catalog, net transfer of blueprint payload.

## Metrics (3 runs)

```
[S3-02] VERIFIER_DONE pass=True nan=False both_moved=True hinge_ok=True
mismatches=0 bytes_a=2241 bytes_b=2227 ser_ms≈0.03–0.35 deser_ms≈0.03–0.24
A_delta≈4.323 B_delta≈3.793 schema=ra2.robot_blueprint.v0
```

- Console: no project errors after compile / Play Mode.
- Compact JSON ~2.2 KB per sample; ser/deser well under 1 ms after warm-up.

## Packages

**None added.** Provisional JSON via built-in `JsonUtility` only. Serialization format remains **provisional** (not frozen).

## Verdict

**S3-02 PASS** — sample blueprints survive file round-trip with 0 critical mismatches; deserialized data still builds and drives. **S3-03 PASS** (loopback net spawn). Exit: [STAGE3_EXIT.md](./STAGE3_EXIT.md).

## Open questions

- When to freeze schema versioning / tolerate unknown fields (U-SER full).  
- JSON vs compact binary for match admit payload size.  
- Shared spawn path for local test + loopback host (S3-03).
