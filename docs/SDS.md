# Software / Technical Design Specification — ra2-analog

**Тип документа:** SDS / TDS (как реализовать системы технически)  
**Не содержит:** production code  
**Терминология:** engine-agnostic, где возможно  
**Связанные документы:** [GDD](GDD.md) · [TECHNICAL_ROADMAP](TECHNICAL_ROADMAP.md) · [TECHNICAL_PRINCIPLES](TECHNICAL_PRINCIPLES.md) · [UNITY_ENGINE](UNITY_ENGINE.md)

### Статус решений

| Тема | Статус в SDS |
|------|----------------|
| Архитектура core-систем | Специфицируется engine-agnostic |
| Networking model для physics | Кандидаты + trade-offs; freeze → `[EXPERIMENT REQUIRED]` |
| Конкретный physics backend / net library | `[EXPERIMENT REQUIRED]` |
| Production engine | См. § ENGINE EVALUATION (ниже). Product docs ранее зафиксировали Unity — evaluation здесь обосновывает рекомендацию и риски |

Если решение нельзя принять без измерения — **`[EXPERIMENT REQUIRED]`** (гипотеза, эксперимент, метрики, критерий успеха, возможные исходы, какое архитектурное решение принимается после).

---

## 0. Architectural principles

1. **Multiplayer-first contracts** — схемы данных и authority boundary существуют до «красивого» локального UX.  
2. **Physics-first** — геймплейные эффекты проходят через симуляцию/события симуляции, не через скрытые cheat-статы.  
3. **Data-driven** — типы компонентов, actions, материалы описываются данными.  
4. **Modular components** — единица конструкции = ComponentInstance.  
5. **Low coupling** — подсистемы общаются через интерфейсы/события/команды, не через прямые зависимости на UI или конкретный net transport.  
6. **Replaceable implementations** — PhysicsBackend, NetTransport, PersistenceStore — порты.  
7. **Testability** — чистая логика (validation, binding resolve, damage apply) без сцены.  
8. **Scalable component count** — budget и LOD sync; запрет O(n²) на каждый компонент без нужды.

### Logical layers

```
Presentation (Editor UI, HUD, VFX)
    ↓ commands / queries
Application (Construction, Configure, Test, Battle, Lobby flows)
    ↓
Domain (RobotDefinition, Actions, Validation, Damage rules, Match rules)
    ↓
Simulation (PhysicsWorld, MotorDrivers, Detach)
    ↓
Infrastructure (NetSession, Persistence, Telemetry, Platform/Engine adapters)
```

Simulation authority in multiplayer battles: **server (or dedicated sim host)**. Clients present and send input.

---

# CORE SYSTEMS

## A. Robot Definition

### Responsibility
Каноническое описание робота: **immutable design** + отдельный **runtime state**.

### Immutable design data (`RobotBlueprint`)
- `blueprintId`, `schemaVersion`, `contentHash`
- `hull` / bounds descriptor
- `materials[]` references
- `nodes[]`: `instanceId`, `typeId`, local transform, material override, params
- `connections[]`: `connectionId`, `a`, `b`, joint/attachment kind, limits
- `controlMap`: bindings, groups, composites
- `metadata`: name, author, createdAt

### Runtime state (`RobotRuntime`)
- pose / velocities per simulated body (or proxy set)
- per-component: health/flags, enabled, attached, power draw
- aggregate: mass, CoM, power budget remaining
- control: last applied command set

### IDs
- `ComponentTypeId` — стабильный строковый/числовой ID определения  
- `ComponentInstanceId` — уникален внутри blueprint  
- Не переиспользовать instanceId после detach/destroy в одном матче

### Ownership
- Design: owned by player profile / local drafts; server validates on admit  
- Runtime in match: **sim host owns**

### Alternatives
| Approach | Pros | Cons |
|----------|------|------|
| Single mutable scene object as source of truth | Fast to hack | Unusable for net/persist |
| Blueprint + runtime split | Clear MP/persist | More plumbing |
| Diff-only runtime patches | Bandwidth | Harder debugging |

**Decision:** Blueprint + Runtime split.  
**Open:** binary vs text serialization → `[EXPERIMENT REQUIRED]` (см. Persistence / STAGE 3).

---

## B. Component System

### Responsibility
Реестр типов, фабрика runtime-модулей, capabilities/actions.

### Model
- `ComponentTypeDef`: properties, mass, collision shape ref, joint anchors, `capabilities[]`, `actions[]`, dependencies  
- `ComponentInstance`: typeId + param overrides  
- `ComponentRuntimeModule`: applies forces/state; does **not** read raw keyboard

### Capabilities (examples)
`ProvidesThrust`, `ProvidesTorque`, `ConsumesPower`, `DealsDamage`, `Structural`, `SteerAble`

### Lifecycle
`Defined → Instantiated → Attached → Active ⇄ Disabled → Detached → Destroyed`

### Ownership
Type defs: content/data. Instances: blueprint. Runtime modules: sim host in battle.

### Scaling
N types data-only; M instances per robot gated by validation budget.

---

## C. Construction System

### Responsibility
Placement, geometry bounds, materials, connections, validation → valid `RobotBlueprint`.

### Flow
Place/edit → update graph → validate → show errors → serialize blueprint.

### Validation (server-identical rules)
- overlap / bounds  
- connection legality  
- mass / CoM warnings  
- power feasibility (static)  
- complexity budget (bodies/joints) from physics envelope  
- unknown typeIds rejected  

### Ownership
Client may preview; **server re-validates** before match.

### Alternatives
Free placement vs grid snap — UX choice; both emit same blueprint schema. Grid may be MVP simplification `[EXPERIMENT REQUIRED]` for usability, not for net.

---

## D. Action System

Data-driven commands available on components/groups.

### Examples
| Type | Actions |
|------|---------|
| Rotor | `rotate_forward`, `rotate_backward`, `brake` |
| Piston | `extend`, `retract`, `stop` |
| Wheel | `forward`, `reverse`, `steering` |
| Weapon | `activate`, `deactivate` |

### ActionDef
- `actionId`, value domain (`bool` / `float[-1,1]`), resource cost hooks, mutual exclusions (optional data)

### Runtime
`ActionCommand { instanceOrGroupId, actionId, value, tick }`  
Resolved by `ActionExecutor` on sim host → motor/weapon drivers.

### Alternatives
Hardcoded per-class methods vs data table — **data table required** by pillars.

---

## E. Input Binding System

### Mapping chain
`PhysicalInput` → `Binding` → `Action` | `CompositeAction` → (optional group expansion) → `ActionCommand[]`

### Concepts
- **Group:** set of instanceIds  
- **Composite:** one input → many actions with coefficients/signs (tank steer)  
- **Modifiers:** shift-layer etc. (post-MVP ok)  
- **Analog:** float values when action domain allows  

### Conflict policy
`[EXPERIMENT REQUIRED]` exact priority rules; must be deterministic and documented. Hypotheses: last-write; axis cancel; explicit priority field on bindings.

### Ownership
Control map in blueprint; execution on sim host.

---

## F. Test Environment

### Responsibility
Local (or offline) harness: spawn robot from same blueprint path as battle; instant reset; reload config; physics world reset.

### Requirements
- No distinct “test-only” robot format  
- Reset restores poses/velocities/component flags to spawn snapshot  
- Fast mode switch with Construction/Configure (see STAGE 6)

### MP
Test may be client-local; blueprint must remain MP-admissible.

---

## G. Physics Simulation

### Port: `PhysicsBackend`
Rigid bodies, shapes, joints/constraints, queries, forces/torques, CCD options, isolation of worlds (test vs battle).

### Requirements (logical)
| Topic | Requirement |
|-------|-------------|
| Rigid bodies | Per structural piece or aggregated proxies — **choice experimental** |
| Collision | Robot↔arena, robot↔robot |
| Joints | Wheel hubs, pistons, rotors as constrained DOF |
| Constraints | Limits from connection defs |
| Mass / inertia / CoM | Derived from parts; updated on detach |
| Forces / torque | From motors/weapons/impacts |
| Motors | Velocity/torque targets from ActionExecutor |
| Suspension | Optional; may be joint springs or specialized constraint |
| Damage coupling | Impulse/event → DamageSystem (not silent HP) |
| Detachment | Remove constraint; body becomes free; mass/CoM recompute |

### Sim tick
Fixed step on authoritative host. Client may interpolate renders.

---

## H. Damage System

### Pipeline
`HitEvent / OverstressEvent` → rules → `DamageEvent` → apply flags → optional `DetachEvent` → telemetry

### MVP target
Functional disable of modules; detach subset if EXP-04 passes.

### Authority
Server applies; clients FX.

---

## I. Battle System

Match rules: spawn, timer, win/lose predicates, weapon allowlist, arena params.  
Evaluated on sim host. Emits `MatchOutcome`.

---

## J. Multiplayer Session

Lifecycle: `Created → Lobby → Loading → InBattle → Results → Closed`  
Owns participants, admitted blueprints, sim host reference, phase clock.

---

## K. Lobby

Ready states, loadout select/confirm, rule display, start when ready predicate met.  
Robot visibility to opponents: **open question**.

---

## L. Matchmaking abstraction

Interface: `requestMatch(playlist) → Session`  
Implementations: LAN/lobby, dedicated queue, stub.  
Application depends on interface only.

---

## M. Synchronization

See § Networking approaches. Concrete strategy after STAGE 2 experiments.

---

## N. Results

Authoritative `MatchSummary`: outcome reason, damage aggregates, component outcomes, duration.  
Clients display; may cache locally.

---

## O. Telemetry

Tech + design events: tick overruns, desync markers, assembly times, match length, disconnect codes.  
No PII beyond necessary ids.

---

## P. Persistence

| Data | MVP | Later |
|------|-----|-------|
| RobotBlueprint + control map | ✓ | |
| Player profile stub | ✓ local | cloud |
| MatchSummary | ✓ | history DB |
| Progression/economy | ✗ | STAGE 14 |

---

## Q. Replay / Spectator (future-ready)

Log authoritative inputs + key events + periodic snapshots.  
Do not build full scrubber in MVP; **do not** design event log that precludes replay.

---

# Networking approaches (physics-focused)

Для модульных роботов с joints/detach **полный deterministic lockstep** всех PhysX/Chaos контактов на разных машинах обычно хрупок (недетерминизм solver, timing). Не утверждаем lockstep как обязательный.

### Options

| Approach | Idea | Pros for this game | Cons for this game |
|----------|------|--------------------|--------------------|
| **A. Authoritative server (no client predict)** | Server simulates; clients send input; receive state | Anti-cheat; one physics truth; simpler mentally | Input lag feel; needs interp |
| **B. Authoritative + client prediction + reconcile** | Client predicts own robot; correct on snapshot | Better responsiveness | Prediction of **foreign contacts / joint graphs** very hard; mispredict storms |
| **C. Snapshot interpolation (remote)** | Smooth others from snapshots | Good for viewing remotes | Doesn't fix local lag alone |
| **D. Deterministic lockstep** | All peers same sim | Low bandwidth if only inputs | Fragile with complex contacts; hitch-sensitive; hard with join-in-progress |
| **E. Hybrid** | Server auth full sim; client predict **only self drive** coarsely; interpolate remotes; reduce synced DOF via proxies | Likely pragmatic | Needs careful proxy design; `[EXPERIMENT REQUIRED]` |

**Working hypothesis (not frozen):** **E hybrid** with strong server authority on contacts/damage; prediction limited to self locomotion aids; remotes interpolated. Freeze only after STAGE 2 metrics.

### `[EXPERIMENT REQUIRED]` — NET-PHYS-MODEL
- **Hypothesis:** Hybrid E yields playable 1v1 at target RTT with agreed desync rate.  
- **Experiment:** EXP-05…08 (roadmap).  
- **Metrics:** outcome agreement, max pose error proxy, mispredict count, playable subjective score, server ms/tick.  
- **Success:** GO criteria STAGE 2.  
- **Outcomes → architecture:**  
  - Success → adopt Hybrid E; document sync surface.  
  - Soft fail → reduce rigidbodies synced (aggregate chassis + key parts).  
  - Hard fail → change physics representation for net (server-only detailed joints; clients kinematic presentation) or change net stack — **STOP content**.

---

# ENGINE EVALUATION

Сравнение **как кандидатов** по требованиям ra2-analog. Архитектура SDS намеренно не зависит от API конкретного движка.

### Criteria matrix (qualitative)

| Criterion | Unity | Unreal Engine | Godot 4 |
|-----------|-------|---------------|---------|
| Physics | Mature PhysX; Havok/DOTS options; wheels/joints known | Chaos powerful; complex tuning; strong destruction storytelling | Jolt/GodotPhysics; improving; less AAA battle-proven at robot-PvP scale |
| Runtime construction | Very common pattern; instantiate hierarchy at runtime | Feasible; Actor spawning; heavier conventions | Feasible; lighter |
| Modular rigid bodies | Common; many tutorials/pitfalls documented | Feasible; Phys substepping/Chaos nuances | Feasible; ecosystem smaller for edge cases |
| Joints | Configurable; quality varies; needs spikes | Rich; can be costly | Adequate; fewer advanced samples |
| Destruction/detach | Manual joint break + RB; not “magic” | Chaos destruction strong for env; modular robot detach still custom | Manual; simpler |
| Multiplayer | NGO / NFE / ecosystem; dedicated server package | Built-in replication, Gameplay Framework; dedicated servers common | Multiplayer API exists; fewer large-scale references |
| Dedicated server | Supported; headless targets | Very strong dedicated server culture | Possible; less ops lore |
| Networking ecosystem | Broad packages + UGS | Deep but opinionated | Smaller |
| Debugging | Good editor; physics debug varies | Excellent profiling suite | Good for size; less deep |
| Iteration speed | Generally fast for gameplay spikes | Heavier editor; slower some loops | Often very fast |
| Tooling | Strong indie↔mid | Strong mid↔AAA | Good indie |
| Performance | Sufficient if budgets held; DOTS path optional complexity | High ceiling; cost of complexity | Good for lighter sims; ceiling TBD for our case |
| Dev complexity | Moderate | High | Lower engine complexity; more custom net/physics work likely |
| Deployment | Familiar pipelines | Familiar; larger artifacts | Simple exports |
| Long-term maintainability | Large talent pool; package churn risk | Long support; upgrade pain | Smaller hiring pool; faster engine evolution risk |

### 1. Recommended engine
**Unity**

### 2. Confidence level
**Medium-high for engine choice** as production platform for this team/repo context; **low-medium for any specific Unity physics+net package combo** until STAGE 1–2 gates.

### 3. Reasons
- Runtime modular construction + RB/joints workflow is well-trodden.  
- Iteration speed suits Design↔Test loop.  
- Dedicated server + multiple netcode options exist to **experiment**.  
- Existing workspace baseline already on Unity 6 — reduces switching cost.  
- Unreal is strong on destruction/servers but higher complexity tax before STAGE 2 proof.  
- Godot attractive for speed/cost but higher risk on networking+complex modular physics at PvP quality without more custom work.

### 4. Risks of recommendation
- PhysX networked modular robots may still fail playability → forced proxy architecture.  
- Package churn / netcode API shifts.  
- False security that “Unity means physics net is solved”.

### 5. Prototype required before **final** engine decision
Да, в смысле **подтверждения жизнеспособности на Unity**, не в смысле обязательного bake-off трёх движков параллельно:

- Минимум: STAGE 1 local physics + STAGE 2 MP physics hard gate on Unity.  
- Полный parallel Unreal/Godot spike — только если STAGE 2 hard-fails *and* root cause is engine-level (not representation/net model).  

**Note:** [UNITY_ENGINE.md](UNITY_ENGINE.md) already records a **product lock** on Unity for production. This evaluation **supports** that lock; it does not authorize Unreal/Godot production tracks unless product revisits the lock after a failed Unity STAGE 2 with engine-level cause.

Internals (physics stack, NGO vs NFE, etc.) remain `[EXPERIMENT REQUIRED]` per UNITY_ENGINE.

---

# Cross-cutting: Error, Logging, Security, Performance hypotheses

### Error handling (global)
- Validation errors → structured codes to UI  
- Sim faults (NaN, explosion) → reset entity or end match with error outcome  
- Net faults → disconnect policy  
- Never trust client blueprint/input

### Logging / telemetry (global)
- Correlation ids: `matchId`, `playerId`, `blueprintHash`  
- Channels: sim, net, validation, session  

### Security (global)
- Server validates blueprint & commands  
- Clamp action values  
- Rate-limit inputs  
- Ignore client-set transforms for authoritative bodies  
- Hash/schema version checks  

### Performance hypotheses (measure; not hardware claims)
| Hypothesis ID | Claim to measure |
|---------------|------------------|
| PERF-H1 | Bodies/joints per robot have a soft cap before fixed-step overrun |
| PERF-H2 | 1v1 server tick affordable on ref machine TBD after profiling |
| PERF-H3 | Blueprint size & command rate dominate early bandwidth more than mesh |
| PERF-H4 | Assembly time must stay within match loading / test iteration budgets |

---

# STAGE 0 — Pre-production / Technical Research

### 1. Objective
Capture requirements; list Unity-internal candidates; define spikes; architecture sketch.

### 2. Scope
Docs, candidate matrix, smoke builds — not gameplay systems.

### 3. Preconditions
GDD, Roadmap, this SDS draft.

### 4. System Architecture
Research only; produce ports list: PhysicsBackend, NetTransport, Serializer, SceneModeController.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|---------------------------|--------|---------|-------|--------------|-----------|-----------|
| RequirementsMatrix | Map GDD→tech needs | Docs | Matrix | Draft/final | — | Session | Design |
| CandidateRegistry | Physics/net options | Research | Ranked candidates | Open | — | Session | Design |
| SpikePlan | EXP definitions | Risks | Backlog | — | UNITY_ENGINE | Session | Design |

### 6. Data Model
Requirement, Candidate, ExperimentSpec, DecisionRecord.

### 7. Runtime Flow
N/A (offline process).

### 8. Interfaces
`DecisionLog.append(DecisionRecord)`.

### 9. Physics Requirements
Define evaluation checklist only (RB, joints, detach, two-robot).

### 10. Multiplayer Requirements
Target model: server-authoritative battle. Clients: input + presentation. No freeze of predict/interp.

### 11. Persistence
Decision log + experiment reports.

### 12. Error Handling
Track blockers in research notes.

### 13. Logging / Telemetry
N/A.

### 14. Testing
Smoke: editor opens; server build target exists.

### 15. Performance
Smoke only.

### 16. Security
N/A.

### 17. Acceptance Criteria
Spike plan with metrics; candidates listed; ports named.

### 18. Technical Risks
Premature package freeze.

### 19. Open Questions
Version pin; physics/net shortlist.

### 20. Alternatives Considered
Big research vs jump to coding — research required by risk profile.

---

# STAGE 1 — Physics Prototype

### 1. Objective
Prove local minimal robot physics (chassis, RB, wheels, motors, collisions, joints).

### 2. Scope
Local sim only; hand or semi-data assembly.

### 3. Preconditions
STAGE 0 exit; provisional PhysicsBackend candidate.

### 4. System Architecture
`TestBedScene` → `RobotAssembler` (thin) → `PhysicsWorld` → `SimpleMotorDriver` ← `DebugInput` (temporary; replace with ActionCommands ASAP).

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| PhysicsWorld | Step sim | dt | contacts | running | PhysicsBackend | match/test | Host local |
| RobotAssemblerThin | Build demo robot | defs | body graph | — | PhysicsWorld | per spawn | Local |
| MotorDriver | Apply wheel/rotor torques | commands | forces | — | PhysicsWorld | per tick | Local |
| DebugInput | Temporary keys | hardware | commands | — | — | editor | Client |

### 6. Data Model
Minimal `NodeSpec`, `JointSpec`, `MotorSpec`.

### 7. Runtime Flow
Load specs → create bodies/joints → tick: input→motors→step→render.

### 8. Interfaces
`IPhysicsBackend`, `IMotorDriver.apply(CommandSet)`.

### 9. Physics Requirements
Full local checklist: RB, collision, joints, mass/CoM, torque motors; suspension optional; damage/detach out of scope except observation.

### 10. Multiplayer Requirements
All local. Prepare: stable IDs; no client-auth transforms later. Prediction N/A.

### 11. Persistence
Optional dump of demo specs.

### 12. Error Handling
Detect NaN/explosion → soft reset.

### 13. Logging / Telemetry
Physics step time, body/joint counts.

### 14. Testing
Unit: mass aggregate. Integration: spawn. Physics: drive/collision. MP: N/A. Stress: EXP-02.

### 15. Performance
Measure PERF-H1 locally.

### 16. Security
N/A.

### 17. Acceptance Criteria
Readable drive+collision; budget yellow line set.

### 18. Technical Risks
Joint instability; wheel model inadequacy.

### 19. Open Questions
Wheel collider vs raycast; aggregation vs per-part RB.

### 20. Alternatives Considered
Per-part RB vs compound — **trade-off detail vs cost**; choose via EXP-02.  
Built-in vehicle helper vs custom motors — custom preferred for modularity.

**`[EXPERIMENT REQUIRED]` EXP-02/03** — see UNITY_ENGINE.

---

# STAGE 2 — Multiplayer Physics Prototype ★

### 1. Objective
Two real clients, two robots, movement, collisions, thin combat, net sync — **hard gate**.

### 2. Scope
Minimal session; no full lobby UX required.

### 3. Preconditions
STAGE 1 GO; net candidate selected for spike.

### 4. System Architecture
```
ClientA/B: InputGatherer → NetClient → 
Server: InputBuffer → ActionExecutor → PhysicsWorld → SnapshotBuilder → NetServer
ClientA/B: SnapshotReceiver → Interpolator → Presentation
```

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| NetSessionSpike | Connect 2 peers | config | connected | phase | Transport | session | Shared |
| InputGatherer | Sample + send | hardware | InputFrame | seq | Control map thin | tick | Client |
| InputBuffer | Order inputs | frames | tick cmds | queues | — | tick | Server |
| SnapshotBuilder | Pack poses/flags | sim | Snapshot | — | Physics | tick/N | Server |
| Interpolator | Smooth remotes | snaps | render poses | buffer | — | frame | Client |
| ThinHitRule | Apply impulse/disable | contacts | events | — | Damage thin | event | Server |

### 6. Data Model
`InputFrame`, `Snapshot`, `PartPose`, `MatchSpikeId`.

### 7. Runtime Flow
Ready → spawn both on server → loop: recv input → sim → snapshot → clients render → thin hit events.

### 8. Interfaces
`INetTransport`, `ISnapshotCodec`, `IInputCodec`.

### 9. Physics Requirements
Same as STAGE 1 on **server**. Clients kinematic/presentation for remote bodies unless experiment says otherwise. Detach optional thin.

### 10. Multiplayer Requirements
| Topic | Spec |
|-------|------|
| Server authority | Physics, hits, disables |
| Client authority | None for transforms; input only |
| Replicated | Poses (or proxies), part enabled flags, score/outcome |
| Non-replicated | Local VFX, camera, raw UI |
| Input sync | Quantized commands with seq/tick |
| Events | Hit/disable reliable channel |
| Snapshots | Periodic state |
| Prediction | Optional self only — experiment |
| Interpolation | Remotes |
| Reconciliation | If prediction enabled |
| Latency | Network simulator mandatory in tests |
| Disconnect | End spike / forfeit policy stub |

Evaluate approaches A–E (§ Networking); do not assume lockstep.

### 11. Persistence
Spike logs only.

### 12. Error Handling
Desync detect → log + abort match with error code.

### 13. Logging / Telemetry
RTT, loss, pose error proxy, tick overrun.

### 14. Testing
Unit: codec. Integration: 2-process join. Physics: server contacts. MP: EXP-05…08. Stress: loss/jitter.

### 15. Performance
Server ms/tick; bandwidth snapshot size — measure PERF-H2/H3.

### 16. Security
Cheat impulse smoke; reject client pose writes.

### 17. Acceptance Criteria
Roadmap STAGE 2 GO metrics.

### 18. Technical Risks
Unplayable lag; desync; server cost.

### 19. Open Questions
Sync surface size; predict or not.

### 20. Alternatives Considered
Listen vs dedicated for spike — listen ok for debug; dedicated smoke still required (EXP-09 thin).  
Full DOF sync vs proxies — proxies if bandwidth/CPU fail.

**STOP if red** — no STAGE 3+ content.

---

# STAGE 3 — Modular Robot Architecture

### 1. Objective
Component system, connections, runtime assembly, data-driven types, lifecycle — net-compatible.

### 2. Scope
Core B + assembler; debug spawn UI only.

### 3. Preconditions
STAGE 2 GO; known sync surface constraints.

### 4. System Architecture
`ComponentTypeRegistry` → `Blueprint` → `RobotFactory` → `RobotRuntime` bound to `PhysicsWorld` + `ReplicationAdapter`.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| TypeRegistry | Load defs | data | TypeDef | loaded | Serializer | boot | Shared |
| RobotFactory | Assemble | Blueprint | Runtime | — | Physics, Registry | spawn | Server in MP |
| ConnectionBuilder | Create joints | connections | joint handles | — | Physics | spawn | Server |
| LifecycleService | Enable/disable/detach | events | state mutations | map | Runtime | event | Server |
| ReplicationAdapter | Map runtime→sync | runtime | snapshots/events | — | Net | tick | Server |

### 6. Data Model
Full RobotBlueprint schema v0; ComponentTypeDef; ConnectionDef; Runtime flags.

### 7. Runtime Flow
Validate blueprint → factory creates bodies → connections → register modules → replicate spawn → tick modules.

### 8. Interfaces
`IRobotFactory`, `IComponentModule`, `ILifecycleService`.

### 9. Physics Requirements
Assembly must set mass/inertia/CoM; joints from data; detach API reserved.

### 10. Multiplayer Requirements
Server spawns from validated blueprint; clients spawn presentation graph from same blueprint + state stream. Input still commands. Prediction unchanged from STAGE 2 decision.

### 11. Persistence
Blueprint serialize round-trip `[EXPERIMENT REQUIRED]` U-SER.

### 12. Error Handling
Failed assembly → reject spawn; log typeId.

### 13. Logging / Telemetry
Assembly ms; body/joint counts; schema version.

### 14. Testing
Unit: registry/validation. Integration: assemble. Physics: stability. MP: net spawn. Stress: max budget robot.

### 15. Performance
Assembly + first seconds after spawn.

### 16. Security
Unknown typeId reject; param clamp.

### 17. Acceptance Criteria
Data-driven robot in local+net; new type without core rewrite.

### 18. Technical Risks
Over-abstraction; schema mismatch sync.

### 19. Open Questions
Param typing system; binary codec.

### 20. Alternatives Considered
ECS vs OOP modules — either ok behind `IComponentModule`; **choose after team spike**, not religiously.  
Scene prefab robots vs pure data — data required.

**`[EXPERIMENT REQUIRED]` U-SER** — round-trip metrics in UNITY_ENGINE.

---

# STAGE 4 — Construction Prototype

### 1. Objective
Player-facing build tools producing validated blueprints.

### 2. Scope
Core C; not final art.

### 3. Preconditions
STAGE 3 schema + budgets.

### 4. System Architecture
`ConstructionEditor` → `BlueprintMutator` → `Validator` → `PreviewAssembler` (local phys optional) → Persist.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| PlacementTool | Transforms | pointer | node edits | draft | — | edit | Client |
| ConnectionTool | Links | picks | connections | draft | — | edit | Client |
| Validator | Rules | blueprint | errors | — | Budgets | on change | Client+Server |
| PreviewAssembler | Optional phys preview | blueprint | preview robot | — | Physics local | edit | Client |
| BlueprintStore | Save/load | blueprint | assets | — | Persistence | — | Client |

### 6. Data Model
DraftBlueprint; ValidationError{code, path}.

### 7. Runtime Flow
Edit → debounce validate → preview → save when clean.

### 8. Interfaces
`IBlueprintValidator`, `IBlueprintStore`.

### 9. Physics Requirements
Preview may approximate; **admit validation** uses same budget numbers as sim envelope. CoM/mass readout required.

### 10. Multiplayer Requirements
Validator shared library. Server admit uses same codepath. No client authority on “legal”.

### 11. Persistence
Save blueprints locally; schemaVersioned.

### 12. Error Handling
Surface codes; block save/admit if critical.

### 13. Logging / Telemetry
Validation fail frequencies; part usage.

### 14. Testing
Unit: validator cases. Integration: save/load. Physics: preview vs runtime parity smoke. MP: tampered blueprint reject. Stress: max parts.

### 15. Performance
Validate & preview on max budget — PERF-H4.

### 16. Security
Server ignores client “validated=true”.

### 17. Acceptance Criteria
≥2 distinct robots to spawn path; CoM feedback works.

### 18. Technical Risks
Editor complexity; illegal joints.

### 19. Open Questions
Grid vs free; hull model.

### 20. Alternatives Considered
Always-phys preview vs ghost mesh — trade accuracy vs iteration speed; measure in UX tests.

---

# STAGE 5 — Control / Configuration System

### 1. Objective
Actions, bindings, groups, composites — Core D+E.

### 2. Scope
Configure UX prototype + resolver used by sim.

### 3. Preconditions
Component actions in data; STAGE 2 command path.

### 4. System Architecture
`BindingEditor` → `ControlMap` in blueprint → `InputGatherer` → `BindingResolver` → `ActionCommand[]` → server executor.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| BindingEditor | UI map | user | ControlMap | draft | Type actions | edit | Client |
| GroupRegistry | Groups | edits | groups | — | — | edit | Blueprint |
| CompositeDefiner | Tank-steer etc. | defs | composites | — | Groups | edit | Blueprint |
| BindingResolver | Expand inputs | InputFrame+Map | commands | — | — | tick | Server (+ local test) |

### 6. Data Model
Binding, Group, CompositeAction, Modifier (optional).

### 7. Runtime Flow
Sample input → resolve → send commands → execute on modules.

### 8. Interfaces
`IBindingResolver`, `IActionExecutor`.

### 9. Physics Requirements
Actions only affect sim via motors/forces; no teleport.

### 10. Multiplayer Requirements
Resolver authoritative on server (client may predict same resolver locally if prediction on). Replicate control map as part of blueprint admit. Analog quantization `[EXPERIMENT REQUIRED]`.

### 11. Persistence
ControlMap inside blueprint.

### 12. Error Handling
Unknown actionId ignored/logged; conflicts per policy.

### 13. Logging / Telemetry
Commands/sec; unresolved bindings.

### 14. Testing
Unit: resolver composites/conflicts. Integration: configure→drive. Physics: torque application. MP: command identity client/server. Stress: spam inputs.

### 15. Performance
Resolver O(bindings) per tick — keep small.

### 16. Security
Clamp values; drop illegal instanceIds.

### 17. Acceptance Criteria
Composite drive + weapon bind works net/local.

### 18. Technical Risks
Determinism of conflicts; UX overload.

### 19. Open Questions
Conflict policy; analog MVP.

### 20. Alternatives Considered
Presets-only vs full binding — full binding is pillar; presets as onboarding overlay.  
Client-side only control — rejected (MP).

**`[EXPERIMENT REQUIRED]` conflict policy playtest.**

---

# STAGE 6 — Seamless Design → Configure → Test

### 1. Objective
Minimal latency mode switching; Test Environment Core F.

### 2. Scope
App shell + Test Room tools; not online test.

### 3. Preconditions
Construction + Control prototypes.

### 4. System Architecture
`AppShell` holds `ActiveBlueprint` + `ModeController` {Design, Configure, Test} → shared `RobotFactory` for test spawns.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| ModeController | Switch modes | request | active mode | mode | Scenes | app | Client |
| TestSession | Spawn/reset | blueprint | runtime | snapshot | Factory, Physics | test | Client |
| ResetService | Restore snapshot | — | restored | — | Physics | on demand | Client |
| DebugOverlays | CoM/actions | runtime | UI | — | — | test | Client |

### 6. Data Model
Mode enum; SpawnSnapshot.

### 7. Runtime Flow
Edit → switch Test (retain blueprint) → spawn → play → reset → switch Design with same blueprint.

### 8. Interfaces
`IModeController`, `ITestSession`.

### 9. Physics Requirements
Isolated test world; full reset of bodies/velocities; reload control map without leak.

### 10. Multiplayer Requirements
Local test. Ensure blueprint path identical to MP admit. No special test types.

### 11. Persistence
In-memory retain; optional autosave drafts.

### 12. Error Handling
Failed spawn → stay in editor with error.

### 13. Logging / Telemetry
Mode switch ms; reset ms (U-SCN).

### 14. Testing
Integration: switch timings. Physics: reset determinism smoke. MP: N/A. Stress: repeated reset.

### 15. Performance
Switch/reset hypotheses — measure, set thresholds from data.

### 16. Security
N/A local.

### 17. Acceptance Criteria
Playtest “no level-load feel”; reset reliable.

### 18. Technical Risks
Hidden loads; state leaks.

### 19. Open Questions
Additive scenes vs single world flags.

### 20. Alternatives Considered
Full reload each test — **rejected** by GDD.  
Separate processes — unnecessary complexity.

**`[EXPERIMENT REQUIRED]` U-SCN** timings.

---

# STAGE 7 — Damage / Destruction

### 1. Objective
Core H: damage, functional failure, detachment path.

### 2. Scope
Rules + apply; FX minimal.

### 3. Preconditions
Lifecycle service; STAGE 2 lessons; EXP-04.

### 4. System Architecture
`Contact/WeaponSignal` → `DamageRules` → `DamageEvent` → `LifecycleService` → `Telemetry` + replication events.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| DamageRules | Map hits→effects | HitEvent | DamageEvent | defs | Data | event | Server |
| IntegrityState | Per-part HP/flags | events | flags | map | — | match | Server |
| DetachExecutor | Break connection | DetachEvent | free body | — | Physics | event | Server |

### 6. Data Model
HitEvent, DamageEvent, DetachEvent, PartIntegrity.

### 7. Runtime Flow
Hit validated → apply disable/detach → recompute mass/CoM → replicate → check battle rules (soft).

### 8. Interfaces
`IDamageRules`, `IDetachExecutor`.

### 9. Physics Requirements
Detach removes joints; updates mass/inertia/CoM; debris collision policy defined (collide vs ignore briefly). Damage may use impulse thresholds.

### 10. Multiplayer Requirements
All apply on server; reliable damage/detach events; snapshots include flags + debris poses if present. Prediction of foreign detach discouraged. Disconnect: as session policy.

### 11. Persistence
Events into match log for results.

### 12. Error Handling
Illegal double-detach no-op; NaN after detach → safe destroy part.

### 13. Logging / Telemetry
Detach counts; disable reasons.

### 14. Testing
Unit: rules table. Integration: shoot wheel→immobile. Physics: EXP-04. MP: both clients same flags. Stress: many detaches.

### 15. Performance
Detach spikes — measure.

### 16. Security
Client cannot fabricate DamageEvent.

### 17. Acceptance Criteria
Readable functional failure in net; detach if GO else disable-only.

### 18. Technical Risks
Desync on detach; cascade soft-lock.

### 19. Open Questions
MVP detach scope.

### 20. Alternatives Considered
HP-only whole robot — rejects pillar.  
Disable-only vs full break — **disable-only fallback** if EXP-04 fails.

---

# STAGE 8 — Combat Prototype

### 1. Objective
Core I: arena, rules, weapons, win/lose, scenarios.

### 2. Scope
One primary rule set; thin weapon set.

### 3. Preconditions
Seamless robot path; damage v0; win condition candidate chosen for prototype.

### 4. System Architecture
`BattleController` + `Arena` + `RuleSet` + `WeaponModules` on server session.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| BattleController | Phase inside match | session | outcomes | phase | Rules, Sim | match | Server |
| RuleSet | Win/lose/timer | events+time | outcome | config | — | match | Server |
| Arena | Collision bounds | — | world | — | Physics | match | Server |
| WeaponModule | activate fire | actions | hits | heat/ammo | Damage | tick | Server |

### 6. Data Model
RuleConfig, ArenaId, OutcomeReason enum.

### 7. Runtime Flow
Load arena → spawn robots → countdown → fight → rule trip → freeze sim → outcome.

### 8. Interfaces
`IRuleSet.evaluate`, `IBattleController`.

### 9. Physics Requirements
Arena colliders; weapon forces/raycasts/projectiles as data-defined; interactions via damage+phys.

### 10. Multiplayer Requirements
Rules server-side; outcome replicated; inputs continue until end; no client win claims.

### 11. Persistence
Outcome stub to summary.

### 12. Error Handling
Both disabled edge → draw/timer rule.

### 13. Logging / Telemetry
Match length; win reasons distribution.

### 14. Testing
Integration scenarios. Physics: weapon push. MP: agreed winner. Stress: long timer match.

### 15. Performance
1v1 envelope from STAGE 2.

### 16. Security
Weapon params from server defs not client.

### 17. Acceptance Criteria
Clear wins; ≥2 scenarios.

### 18. Technical Risks
Stalemate; weapon meta.

### 19. Open Questions
Exact win condition freeze for MVP.

### 20. Alternatives Considered
Stock destruction vs functional disable win — pick via playtest.  
Projectile vs hitscan — data-driven both behind weapon module.

---

# STAGE 9 — Full Multiplayer Battle

### 1. Objective
Core J/K/L + hardened sync: lobby, session, MM abstraction, disconnect, results sync, dedicated server path.

### 2. Scope
Vertical slice flow; MM impl may be lobby-only.

### 3. Preconditions
Combat prototype; STAGE 2 invariants held.

### 4. System Architecture
`Matchmaker` → `Session` → `Lobby` → admit blueprints → `BattleController` → `ResultsPublisher` on dedicated sim host.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| SessionService | Lifecycle | requests | session | phase | Transport | session | Server |
| LobbyService | Ready/loadouts | player msgs | start gate | ready map | Session | lobby | Server |
| MatchmakingPort | Find session | playlist | session id | — | Impl | request | Client/Server |
| AdmitService | Validate blueprints | blueprint | accept/deny | — | Validator | lobby | Server |
| DisconnectPolicy | Handle drop | events | outcome mods | — | Rules | match | Server |
| ResultsPublisher | Push summary | outcome | summary | — | Results | end | Server |

### 6. Data Model
SessionId, PlayerSeat, ReadyState, AdmitRejectCode, DisconnectCode.

### 7. Runtime Flow
Queue/lobby → ready → admit → load → battle → summary → close.

### 8. Interfaces
`IMatchmakingPort`, `ISessionService`, `IDisconnectPolicy`.

### 9. Physics Requirements
Same auth sim; ensure load brings full assembly before go.

### 10. Multiplayer Requirements
Full table: server auth sim+rules; client input+UI; replicate poses/flags/phase/summary; non-replicate cosmetics; input sync; events damage; snapshots; prediction per STAGE 2 freeze; interp remotes; reconcile if any; latency via simulator in QA; disconnect per policy (forfeit/pause/reconnect window — **open**, must be explicit).

### 11. Persistence
Summary store; optional robot cloud later.

### 12. Error Handling
Admit fail messages; host migration **out of scope** unless experiment — prefer dedicated.

### 13. Logging / Telemetry
Phase timings; disconnect codes; admit rejects.

### 14. Testing
Integration full flow. MP soak. Stress 1v1 duration. Security admit tests.

### 15. Performance
EXP-09 server cost model.

### 16. Security
Admit+command validation; anti-spam ready; blueprint hash.

### 17. Acceptance Criteria
Two players complete flow without scene hacks; summary both sides.

### 18. Technical Risks
Edge cases; long desync creep.

### 19. Open Questions
Reconnect; opponent blueprint visibility.

### 20. Alternatives Considered
P2P host vs dedicated — dedicated target; P2P only debug.  
In-engine MM vs external service — hide behind port.

**`[EXPERIMENT REQUIRED]` disconnect policy playtest + EXP-09.**

---

# STAGE 10 — Results / Telemetry

### 1. Objective
Core N+O: post-match stats; telemetry foundations; history-ready schema.

### 2. Scope
Summary UI + event aggregation; not full replay UX.

### 3. Preconditions
Authoritative event stream from battle/damage.

### 4. System Architecture
`EventLog` → `StatsAggregator` → `MatchSummary` → UI + `TelemetrySink` + `SummaryStore`.

### 5. Components
| Component | Responsibility | Inputs | Outputs | State | Dependencies | Lifecycle | Ownership |
|-----------|----------------|--------|---------|-------|--------------|-----------|-----------|
| EventLog | Append-only | events | log | buffer | — | match | Server |
| StatsAggregator | Reduce | log | summary | — | — | end | Server |
| SummaryStore | Persist | summary | id | — | Persistence | after | Server/Client |
| TelemetrySink | Export metrics | events | backend | — | — | always | Infra |
| ResultsUI | Display | summary | — | — | — | results | Client |

### 6. Data Model
MatchSummary, ComponentStatLine, TelemetryEvent.

### 7. Runtime Flow
During match append → on end aggregate → replicate summary → show → store.

### 8. Interfaces
`IEventLog`, `IStatsAggregator`, `ITelemetrySink`.

### 9. Physics Requirements
N/A beyond consuming phys-derived events.

### 10. Multiplayer Requirements
Summary authoritative; clients display only; no client recompute as truth.

### 11. Persistence
Summaries; future history list.

### 12. Error Handling
Partial log → best-effort summary + flag `incomplete`.

### 13. Logging / Telemetry
Meta about missing events.

### 14. Testing
Unit aggregations. Integration match→UI. MP identical summaries. Stress large event counts.

### 15. Performance
Aggregation ≪ sim cost.

### 16. Security
No client-authored stats.

### 17. Acceptance Criteria
Player sees win reason + key part failures.

### 18. Technical Risks
Missing instrumentation.

### 19. Open Questions
History backend.

### 20. Alternatives Considered
Client-side stat guess — rejected.  
Full replay now — defer; keep log future-ready.

---

# STAGE 11 — MVP

### 1. Objective
Integrate A–P (Q future-ready only) into one playable product per GDD MVP.

### 2. Scope
Blessed content set; one MP path; seamless loop.

### 3. Preconditions
Exits stages 6–10 (7 at least disable).

### 4. System Architecture
Composition root wiring all ports; feature flags for unfinished edges.

### 5. Components
`GameComposer` / installers; `MvpChecklist` automation; smoke harness.

### 6. Data Model
Frozen MVP schema version.

### 7. Runtime Flow
Cold boot → Design→Configure→Test→Lobby→Battle→Results→Design.

### 8. Interfaces
All prior; stable facade versions.

### 9. Physics Requirements
Hold STAGE 1–2 envelope on MVP catalog.

### 10. Multiplayer Requirements
Real 1v1 path required; hybrid model as frozen; disconnect policy documented.

### 11. Persistence
Robots + summaries local minimum.

### 12. Error Handling
Player-visible fatal codes; no silent desync.

### 13. Logging / Telemetry
MVP funnel events.

### 14. Testing
Full suite smoke; MP playtest protocol; stress budgets.

### 15. Performance
Document measured envelopes as hypotheses→accepted budgets.

### 16. Security
Admit+input audit pass.

### 17. Acceptance Criteria
GDD §17 + roadmap MVP must-haves.

### 18. Technical Risks
Integration regressions vs STAGE 2.

### 19. Open Questions
Cut list if unstable.

### 20. Alternatives Considered
Ship editor-only — **rejected** (not Robot Arena-like).

---

# STAGE 12 — Production Hardening

### 1. Objective
Optimization, net stability, persistence hardening, QA, security, telemetry SLOs.

### 2. Scope
No new gameplay systems.

### 3. Preconditions
MVP accepted.

### 4. System Architecture
Same; add soak runners, validators fuzz, metrics dashboards.

### 5. Components
PerfProfiler hooks; FuzzAdmit; SoakDriver; CrashReporter.

### 6. Data Model
SLO definitions (measured).

### 7. Runtime Flow
CI soak + playtests → defects → fix.

### 8. Interfaces
Unchanged; tighten contracts.

### 9. Physics Requirements
Solver stability under soak; no creeping NaNs.

### 10. Multiplayer Requirements
Desync rate SLO; disconnect fairness; bandwidth ceilings from measurement.

### 11. Persistence
Migrate schema safely; backup robots.

### 12. Error Handling
Graceful degrade; assert→telemetry in production builds policy.

### 13. Logging / Telemetry
Expand; sampling strategy.

### 14. Testing
Regression + fuzz + soak + security suite.

### 15. Performance
Meet documented SLOs; optimize hotspots only with evidence.

### 16. Security
Pen-test admit/input; rate limits.

### 17. Acceptance Criteria
Critical bugs cleared; soak green.

### 18. Technical Risks
Gold-plating; security theater.

### 19. Open Questions
Live ops tooling depth.

### 20. Alternatives Considered
Big-bang rewrite vs incremental — incremental unless STAGE 2 broken.

---

# STAGE 13 — Content Expansion

### 1. Objective
More types/arenas/modes via data on stable core.

### 2. Scope
Content + playlist configs; no economy power.

### 3. Preconditions
Hardening bar or explicit risk accept.

### 4. System Architecture
Content pipeline → registry → same factories/validators with cost tags.

### 5. Components
ContentPack loader; PartCostAccountant; ModeConfig.

### 6. Data Model
ContentPack manifest; compatibility min schema.

### 7. Runtime Flow
Load pack → validate → available in construction/playlists.

### 8. Interfaces
`IContentPack`.

### 9. Physics Requirements
Each part declares cost estimate; reject over budget.

### 10. Multiplayer Requirements
Same authority; playlist part allowlists enforced server-side.

### 11. Persistence
Versioned packs; blueprint compatibility.

### 12. Error Handling
Missing pack → clear error.

### 13. Logging / Telemetry
Part pick rates; crash tags by pack.

### 14. Testing
Per-part smoke; MP with new parts; stress max legal builds.

### 15. Performance
Re-measure envelope per pack.

### 16. Security
Signed/known packs; reject unknown typeIds.

### 17. Acceptance Criteria
Pipeline adds part without core rewrite; budgets hold.

### 18. Technical Risks
Creep; hidden perf bombs.

### 19. Open Questions
Mode roadmap priority.

### 20. Alternatives Considered
Hardcoded new parts in engine code — rejected.

---

# STAGE 14 — Optional Career / Economy

### 1. Objective
Optional progression layer isolatable from fair PvP.

### 2. Scope
Inventory/unlocks/cosmetics; **no** mandatory P2W.

### 3. Preconditions
Core PvP proven (MVP+).

### 4. System Architecture
`EconomyService` behind flag; `PlaylistPolicy` fair vs casual; server enforce loadouts.

### 5. Components
InventoryStore; UnlockTable; PlaylistPolicy; PurchasePort (optional).

### 6. Data Model
ItemId, Inventory, PlaylistRules (allowed parts).

### 7. Runtime Flow
Menu economy → select loadout → admit checks playlist → match unchanged.

### 8. Interfaces
`IEconomyService`, `IPlaylistPolicy`.

### 9. Physics Requirements
None beyond existing part defs.

### 10. Multiplayer Requirements
Server authoritative inventory checks; fair playlist ignores paid power.

### 11. Persistence
Inventory cloud/local.

### 12. Error Handling
Denied loadout messages.

### 13. Logging / Telemetry
Economy funnel; fair playlist audits.

### 14. Testing
Policy unit tests; MP reject illegal loadout; no physics change tests needed.

### 15. Performance
Negligible.

### 16. Security
No client trust for owned items.

### 17. Acceptance Criteria
Core loop works with economy disabled; fair playlist auditable.

### 18. Technical Risks
Trust; pressure to sell power.

### 19. Open Questions
Whether to ship at all.

### 20. Alternatives Considered
Power unlocks in ranked — **rejected**. Cosmetics / separate casual brackets — preferred.

---

## Global Alternatives log (architecture)

| Decision | Alt 1 | Alt 2 | Alt 3 | Notes |
|----------|-------|-------|-------|-------|
| Net physics | Auth+interp | Auth+predict hybrid | Lockstep | Prefer hybrid pending EXP |
| Robot truth | Blueprint+runtime | Scene-only | Diff-only | Blueprint+runtime |
| Modules | Data+IComponentModule | Hardclass forest | Full ECS mandated | Port allows either impl |
| Test format | Same blueprint | Separate prefab | | Same blueprint required |
| Engine | Unity | Unreal | Godot | See evaluation |

---

## Document maintenance

После каждого Decision Gate обновлять:  
- Decision log in [UNITY_ENGINE.md](UNITY_ENGINE.md)  
- Frozen networking approach subsection here  
- Open Questions → closed with links to experiment reports in `docs/experiments/`
