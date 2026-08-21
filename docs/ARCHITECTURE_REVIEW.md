# Architecture Review — ra2-analog

**Role:** Principal Game / Network / Physics Engineer  
**Inputs:** [GDD](GDD.md) · [TECHNICAL_ROADMAP](TECHNICAL_ROADMAP.md) · [SDS](SDS.md)  
**Mode:** defect-first review — документы **не** переписывались  
**Focus:** особенно кейсы «локальная физика отличная → PvP синхронизация невозможна/неприемлема»

---

## CRITICAL ISSUES

### C1. STAGE 2 gate не доказывает сетевой control model игры

**SEVERITY:** Critical  

**PROBLEM:**  
Roadmap/SDS делают STAGE 2 hard-gate на «два робота едут и сталкиваются», при этом groups/composite actions (ядро фантазии GDD) полноценно появляются только на STAGE 5. STAGE 2 допускает thin/debug input.

**WHY IT MATTERS:**  
Можно получить зелёный GATE 2 на debug-клавишах и позже обнаружить, что composite command expansion + частичные disable + latency ломают пилотирование — уже после вложений в Construction UX.

**AFFECTED SYSTEMS:** Control / Input Binding, Sync, STAGE 2 gate, MVP loop  

**RECOMMENDATION:**  
В STAGE 2 обязателен **минимальный composite drive** (хотя бы hard-coded tank WASD → 4 wheel commands), исполняемый тем же `ActionCommand` путём, что и production. UI редактора groups можно отложить; **command path — нет**.

**VALIDATION:**  
Net playtest: только composite drive под 60–100 ms RTT + 1–2% loss; сравнить с raw per-wheel debug binds.

---

### C2. Sync surface STAGE 2 может быть обесценен STAGE 3/7 (скрытая зависимость)

**SEVERITY:** Critical  

**PROBLEM:**  
STAGE 2 GO фиксирует «работающую» sync-модель на упрощённых роботах. STAGE 3 вводит data-driven runtime assembly; STAGE 7 — disable/detach с пересчётом mass/CoM и, возможно, debris bodies. Это меняет число DOF, события и mid-match topology.

**WHY IT MATTERS:**  
Классический failure mode: локально и на spike всё зелёное; после modular+damage — desync/jitter/unaffordable snapshots. Команда уже построила Construction/Control на ложном GO.

**AFFECTED SYSTEMS:** ReplicationAdapter, Physics, Damage, RobotFactory, Roadmap gates  

**RECOMMENDATION:**  
GATE 2 считать **provisional**. Обязательные **re-gates**:  
- после STAGE 3 (data-spawn net),  
- после STAGE 7 (damage/detach net).  
Любой fail = STOP контента, как STAGE 2.

**VALIDATION:**  
Повторить EXP-05…08 на max-budget data robot и на сценарии с mid-match disable/detach.

---

### C3. Локально стабильная per-part RB + joints почти наверняка не равна «PvP-syncable sim»

**SEVERITY:** Critical  

**PROBLEM:**  
SDS/Roadmap допускают (и STAGE 1 поощряет) детальную jointed simulation как truth. Networking section правильно сомневается в lockstep и prediction, но **архитектура всё ещё предполагает**, что server может симулировать тот же fidelity, а клиенты — приемлемо отображать. Не доказано, что:

- контакты двух jointed машин fair/readable online;  
- snapshotting многих bodies укладывается в bandwidth/CPU;  
- visual interpolation не ломает ощущение жёстких связей.

**WHY IT MATTERS:**  
Это ровно тот риск, от которого roadmap якобы защищает — но gate может пройти на 2–3 bodies/robot и не экстраполироваться на construction freedom GDD.

**AFFECTED SYSTEMS:** PhysicsBackend, Sync, Construction budgets, Combat feel  

**RECOMMENDATION:**  
Явно разделить **SimRepresentation** vs **PresentationRepresentation**. Заранее спроектировать fallback: server detailed / client proxy или aggregated chassis + vital parts. Construction validator должен ограничивать **sync-cost**, не только «local physics ms».

**VALIDATION:**  
Матрица: (bodies/joints) × (1v1 net) → playable score, pose error, server ms/tick, kb/s. Найти cliff, где local OK, net NOT OK.

---

### C4. Win condition MVP не зафиксирован — Combat/Results/MP session нельзя корректно специфицировать

**SEVERITY:** Critical (product/architecture)  

**PROBLEM:**  
GDD и SDS оставляют win condition open, но MVP требует Battle + Results + authoritative outcome. Без правила «что есть смерть» damage model (disable vs detach vs whole-robot) и sync приоритеты плавают.

**WHY IT MATTERS:**  
Нельзя писать RuleSet, anti-stall, disconnect-forfeit, stats, tests. Команды будут импровизировать три несовместимых критерия.

**AFFECTED SYSTEMS:** Battle, Damage, Results, Session, Telemetry  

**RECOMMENDATION:**  
До STAGE 8 (лучше до конца STAGE 2 thin combat) заморозить **одно** MVP rule, например:  
`FunctionalElimination` = не может двигаться И не может активировать оружие N секунд, OR timer score.  
Detach-kill — отдельно post-MVP.

**VALIDATION:**  
10 внутренних боёв: игроки согласны с исходом без споров «я ещё жив».

---

## HIGH PRIORITY

### H1. Противоречие messaging: engine lock vs SDS «не выбирай заранее»

**SEVERITY:** High  

**PROBLEM:**  
GDD / UNITY_ENGINE / Roadmap: Unity production fixed. SDS Engine Evaluation снова сравнивает Unreal/Godot и говорит о prototype before final decision.

**WHY IT MATTERS:**  
Путаница scope: трата времени на multi-engine bake-off или, наоборот, игнор Unity-internal spikes.

**AFFECTED SYSTEMS:** STAGE 0, staffing, docs governance  

**RECOMMENDATION:**  
Считать **engine locked**; SDS evaluation — rationale archive. Открыты только Unity-internal experiments. Parallel engine только после engine-level fail STAGE 2.

**VALIDATION:**  
Doc decision record: one paragraph «engine closed / packages open».

---

### H2. Dual physics pipelines: Construction preview ≈ «может отличаться» от server sim

**SEVERITY:** High  

**PROBLEM:**  
SDS STAGE 4: preview may approximate; admit uses budget numbers. GDD pillar = physics truth in design loop. Расхождение preview vs battle — прямой путь к «в тесте ехало, в PvP нет».

**WHY IT MATTERS:**  
Разрушает trust iteration loop; провоцирует hidden second physics stack (massive rewrite later).

**AFFECTED SYSTEMS:** Construction, Test, Physics, MP admit  

**RECOMMENDATION:**  
Test Room и battle spawn — **один** `RobotFactory` + один backend config. Preview в редакторе: либо тот же factory в изоляции, либо явный non-physics ghost (без претензии на behaviour).

**VALIDATION:**  
Same blueprint: editor-preview (if phys) vs Test vs server smoke — compare CoM, first-second drive response.

---

### H3. Groups/composites помечены как система STAGE 5, но для GDD это не «optional subsystem»

**SEVERITY:** High  

**PROBLEM:**  
Без composite groups игра ближе к «прибил WASD к пресету», не к Control Programming pillar.

**WHY IT MATTERS:**  
MVP без groups проваливает GDD uniqueness vs ordinary robot combat.

**AFFECTED SYSTEMS:** Control, GDD pillars, MVP checklist  

**RECOMMENDATION:**  
MVP MUST: groups + composites. UI can be crude. См. C1 — net path early.

**VALIDATION:**  
Playtest question: «чем управление вашего робота уникально?» — нужен ненулевой % ответов про схему, не только форму.

---

### H4. Client-side BindingResolver prediction без гарантии идентичности

**SEVERITY:** High  

**PROBLEM:**  
SDS допускает client resolve «same as server» for prediction. Float params, conflict policy open, partial disables mid-match → classic command divergence.

**WHY IT MATTERS:**  
Mispredict storms на собственном роботе при контактах — хуже, чем честный lag.

**AFFECTED SYSTEMS:** Input, Sync, Control, Damage interaction  

**RECOMMENDATION:**  
MVP networking: **authoritative + interpolation, prediction OFF** (или predict только «желаемые motor targets» без локального collision resolve). Включать prediction только после EXP-08 на disabled-parts scenarios.

**VALIDATION:**  
A/B: predict on/off under 80 ms RTT while hitting walls/enemies; count visible corrections / player preference.

---

### H5. Power budget объявлен в GDD/Design system, но как runtime system в SDS почти отсутствует

**SEVERITY:** High  

**PROBLEM:**  
GDD: мощность — первоклассное ограничение дизайна. SDS упоминает static power feasibility и `power draw` state, но нет явной PowerNetwork / allocation tick / failure mode.

**WHY IT MATTERS:**  
Либо infinite motors (ломает engineering), либо ad-hoc ifs в MotorDriver (coupling + rewrite).

**AFFECTED SYSTEMS:** Construction validation, Motors, Weapons, Damage (battery), Data model  

**RECOMMENDATION:**  
Минимальный MVP power model: `SupplyCapacity` sum vs `PeakDraw` sum; if over — scale or deny activate (data rule). Runtime tick optional post-MVP; static check обязателен.

**VALIDATION:**  
Build over-budget robot: cannot admit / motors limp in Test per rule; under-budget OK.

---

### H6. Runtime assembly после net spike = запланированный rewrite риск

**SEVERITY:** High  

**PROBLEM:**  
STAGE 1–2 = thin/hand assembly; STAGE 3 replaces with data factory. Roadmap acknowledges; SDS underestimates how much replication/physics tuning is one-off on hand robots.

**WHY IT MATTERS:**  
Потерянные недели joint tuning; regressions returning to STAGE 2 red.

**AFFECTED SYSTEMS:** RobotFactory, STAGE 1–3  

**RECOMMENDATION:**  
Не позже середины STAGE 1: **один** blueprint JSON/binary на demo robot; STAGE 2 spawns only via factory stub. Hand hierarchy forbidden in net spike.

**VALIDATION:**  
STAGE 2 robot loaded from serialized blueprint only; no scene-placed joints.

---

### H7. Detach как «optional EXP» конфликтует с ожиданиями physical destruction fantasy

**SEVERITY:** High (design/tech)  

**PROBLEM:**  
GDD damage states include detached; matrix marks detach post-MVP; SDS EXP-04 may enable subset. Marketing/design language всё ещё «physical destruction». Net detach — один из самых опасных sync кейсов.

**WHY IT MATTERS:**  
Либо over-promise, либо mid-MVP scope creep в нерешаемую sync задачу.

**AFFECTED SYSTEMS:** Damage, Sync, Results, GDD communication  

**RECOMMENDATION:**  
Публичный MVP contract: **functional disable + visual break FX without free debris authority**. True detach = post-MVP gated. Update GDD language consistency.

**VALIDATION:**  
Playtest: disable-only fights still feel like Robot Arena? If no, limited detach on non-structural parts only under EXP-04.

---

### H8. Seamless Design↔Configure↔Test стоит после Construction/Control — правильно по зависимостям, опасно по product risk

**SEVERITY:** High  

**PROBLEM:**  
GDD: iteration loop — сердце опыта. Roadmap STAGE 6 поздно на critical path. Можно увлечься системами и обнаружить, что Unity load/phys reset не даёт «бесшовности».

**WHY IT MATTERS:**  
Pillar Fast Iteration провален → GDD fantasy мёртв даже при working PvP.

**AFFECTED SYSTEMS:** ModeController, scenes, Test, UX  

**RECOMMENDATION:**  
Thin **STAGE 6 spike parallel** сразу после STAGE 3 (empty modes + spawn/reset timings), не ждать полного Construction UX polish.

**VALIDATION:**  
U-SCN early: switch/reset timings on stub blueprint before STAGE 4 art.

---

### H9. Snapshot-all-bodies bandwidth/CPU — bottleneck by default

**SEVERITY:** High  

**PROBLEM:**  
SDS snapshots poses; no hard rule that MVP syncs only budgeted proxy set. Construction freedom pushes part count up.

**WHY IT MATTERS:**  
Server scalability collapses; clients choke; team «оптимизирует сеть» после контента.

**AFFECTED SYSTEMS:** Sync, Server, Construction limits  

**RECOMMENDATION:**  
`SyncBudget` per robot (max replicated bodies/rates) enforced by validator. LOD: full rate for chassis, low rate for cosmetics/debris.

**VALIDATION:**  
PERF bandwidth test at max legal build 1v1; fail if over agreed kb/s hypothesis.

---

### H10. Cheat: collider/mass/param underreporting in blueprint

**SEVERITY:** High  

**PROBLEM:**  
SDS stresses invalid configs and pose writes; weaker on **physical param forgery** (tiny colliders, absurd friction, inverted mass) that still «validates schema».

**WHY IT MATTERS:**  
PvP immediately warps into exploit assembly; skill-based pillar dies.

**AFFECTED SYSTEMS:** Validator, Admit, Security, Persistence  

**RECOMMENDATION:**  
Server recomputes mass/inertia from **authoritative type defs + allowed overrides whitelist**. Client-supplied mass ignored. Clamp scales/friction; reject non-catalog shapes.

**VALIDATION:**  
Fuzz blueprints with extreme overrides; admit must reject or normalize; match uses server-rebuilt stats.

---

## MEDIUM PRIORITY

### M1. Conflict policy for bindings — open, but composites are MVP-critical

**SEVERITY:** Medium  

**PROBLEM:**  
SDS marks conflict policy `[EXPERIMENT REQUIRED]` while depending on deterministic resolve for MP.

**WHY IT MATTERS:**  
Heisenbugs «почему робот дрейфует» online.

**AFFECTED SYSTEMS:** BindingResolver, Net, UX  

**RECOMMENDATION:**  
Freeze v0 before STAGE 5 net tests: axis cancel + explicit priority int; document in GDD control section.

**VALIDATION:**  
Unit matrix of conflicting binds; identical client/server golden vectors.

---

### M2. Materials «живучесть» в GDD MVP vs SDS focus on mass/CoM

**SEVERITY:** Medium  

**PROBLEM:**  
GDD MVP materials affect mass and durability; SDS construction validation barely models durability tiers.

**WHY IT MATTERS:**  
Либо мёртвая GDD feature, либо late damage coupling rewrite.

**AFFECTED SYSTEMS:** Materials, Damage, Construction  

**RECOMMENDATION:**  
MVP: 2–3 material multipliers on HP/break threshold only; or cut durability from MVP text until STAGE 7.

**VALIDATION:**  
Same shape, two materials → measurable integrity difference in Test.

---

### M3. Excessive coupling risk: Blueprint as god-document

**SEVERITY:** Medium  

**PROBLEM:**  
Blueprint несёт hull, parts, joints, control map, metadata. Correct for atomic loadout, but invites UI/systems reaching into one blob.

**WHY IT MATTERS:**  
Tight coupling Construction↔Control↔Persist; merge conflicts; hard testing.

**AFFECTED SYSTEMS:** Data architecture, all editors  

**RECOMMENDATION:**  
Logical sections with validated sub-schemas; APIs mutate via `BlueprintMutator` only; no raw dict poking from UI.

**VALIDATION:**  
Architectural test: Control editor cannot reference PlacementTool types.

---

### M4. Over-abstract data-driven framework before 3 concrete parts

**SEVERITY:** Medium  

**PROBLEM:**  
SDS correctly requires data-driven; risk of building capability ECS cathedral in STAGE 3.

**WHY IT MATTERS:**  
Delayed STAGE 2 re-gates; unreadable code; false security.

**AFFECTED SYSTEMS:** ComponentSystem, velocity of MVP  

**RECOMMENDATION:**  
STAGE 3: ≥4 real types (wheel, chassis, motor/rotor, weapon) data-driven; capabilities minimal. No generic scripting.

**VALIDATION:**  
Time-to-add 5th type &lt; agreed threshold without core edits.

---

### M5. Listen-server habits → dedicated rewrite

**SEVERITY:** Medium  

**PROBLEM:**  
Spike allows listen-server; production dedicated. Easy to couple sim to «host player object».

**WHY IT MATTERS:**  
Host migration fantasies; headless fails; input special-casing.

**AFFECTED SYSTEMS:** Session, Sim host, STAGE 9  

**RECOMMENDATION:**  
Code rule: sim host has no local player privilege path; even listen mode uses same InputBuffer.

**VALIDATION:**  
Dedicated headless smoke EXP-09 every milestone after STAGE 2.

---

### M6. Persistence schema evolution under-specified

**SEVERITY:** Medium  

**PROBLEM:**  
schemaVersion mentioned; migrations, float stability for `contentHash`, control map atomicity thin.

**WHY IT MATTERS:**  
Broken robots after content patches; hash mismatches; MP admit chaos.

**AFFECTED SYSTEMS:** Persistence, Admit, Content packs  

**RECOMMENDATION:**  
Hash canonical serialization (sorted keys, quantized transforms). Migration functions per version. Reject unknown versions server-side.

**VALIDATION:**  
Round-trip + version bump fixture tests; mutated float noise doesn't flip hash incorrectly if quantized.

---

### M7. Results depend on event instrumentation that Combat may forget

**SEVERITY:** Medium  

**PROBLEM:**  
STAGE 10 assumes EventLog richness; STAGE 8 may only emit outcome.

**WHY IT MATTERS:**  
GDD «понял почему проиграл» fails; telemetry blind.

**AFFECTED SYSTEMS:** Results, Damage, Telemetry  

**RECOMMENDATION:**  
Minimum event set frozen with STAGE 7/8: `PartDisabled`, `PartDetached?`, `DamageApplied`, `MatchEnded(reason)`.

**VALIDATION:**  
After fight, summary always names ≥1 decisive part event or explicit `timeout`.

---

### M8. Server scalability: no match density model

**SEVERITY:** Medium  

**PROBLEM:**  
PERF hypotheses exist; no architecture for N concurrent matches / affinity / physics world isolation cost.

**WHY IT MATTERS:**  
1v1 demo ≠ service.

**AFFECTED SYSTEMS:** Dedicated server, Session  

**RECOMMENDATION:**  
Post-STAGE 9: define `MatchesPerCore` hypothesis from EXP-09; isolate worlds; forbid shared default physics scene.

**VALIDATION:**  
Soak K parallel 1v1 headless until tick overrun rate exceeds SLO.

---

### M9. Interpolation vs rigid joints visual break

**SEVERITY:** Medium  

**PROBLEM:**  
Per-body interpolation can separate visually constrained parts.

**WHY IT MATTERS:**  
Looks desynced even when server OK — players blame net.

**AFFECTED SYSTEMS:** Presentation, Sync  

**RECOMMENDATION:**  
Replicate/interpolate **root + relative poses** or whole-actor snapshot; avoid independent per-child smooth without hierarchy.

**VALIDATION:**  
Film remote robot at 100 ms RTT; measure joint separation error visually/quantitatively.

---

### M10. Soft contradiction: GDD MVP «хрупкий MP задел» vs Roadmap hard-stop

**SEVERITY:** Medium (docs)  

**PROBLEM:**  
GDD §17.6 допускает хрупкий online path; Roadmap forbids progress without satisfactory physics+MP.

**WHY IT MATTERS:**  
Stakeholders may push «давайте редактор, сеть потом» цитируя GDD.

**AFFECTED SYSTEMS:** Governance  

**RECOMMENDATION:**  
GDD defer to Roadmap hard-stop for sequencing; GDD wording = quality bar inside MP spike, not permission to skip.

**VALIDATION:**  
N/A — clarify in next doc pass (review only flags it).

---

## LOW PRIORITY

### L1. Analog input optional ambiguity

**SEVERITY:** Low  

**PROBLEM:**  
GDD/SDS leave analog open; composite drive often feels worse digital-only on sticks later.

**RECOMMENDATION:**  
MVP keyboard digital; design action values as float domain anyway.  

**VALIDATION:**  
N/A early.

---

### L2. Replay/spectator future-ready pressure

**SEVERITY:** Low  

**PROBLEM:**  
Useful caution; can over-engineer EventLog.

**RECOMMENDATION:**  
Append-only compact events; no scrubber UI.  

**VALIDATION:**  
Log size per match hypothesis.

---

### L3. Suspension listed in physics requirements without MVP ownership

**SEVERITY:** Low  

**PROBLEM:**  
SDS physics table includes suspension; MVP may not need.

**RECOMMENDATION:**  
Explicitly out of MVP unless wheel model demands.  

**VALIDATION:**  
Drive quality without suspension acceptable?

---

### L4. Matchmaking abstraction before one playlist exists

**SEVERITY:** Low–Medium borderline  

**PROBLEM:**  
Good port, but easy to gold-plate.

**RECOMMENDATION:**  
Interface + lobby impl only for MVP.  

**VALIDATION:**  
N/A.

---

## Extra findings mapped to checklist (11–20)

| # | Finding summary | Sev |
|---|-----------------|-----|
| 11 Performance | H9 snapshots; C3 part counts; assembly spikes | High/Crit |
| 12 Scalability | M8 match density | Medium |
| 13 Cheats | H10 params; disconnect grief (policy open) | High |
| 14 Persistence | M6 schema/hash | Medium |
| 15 Runtime construction | H6 factory-late; first-frame joint explosion | High |
| 16 Damage/detach | H7; C2 re-gate | High/Crit |
| 17 Phys sync | C3; H4; M9 | Critical/High |
| 18 Seamless loop | H8 late discovery | High |
| 19 Cut from MVP | see below | — |
| 20 Falsely optional | see below | — |

---

## Features to remove / defer from MVP

- True physical **detach/debris authority** (keep disable + FX)  
- **Client prediction** of full self physics  
- Analog/gamepad polish  
- Material science beyond 2–3 multipliers (or cut)  
- Suspension system as feature  
- Reconnect  
- Skill-based matchmaking / multiple modes  
- Spectator/replay UI  
- Economy/career hooks  
- Free-form advanced hull editor (prefer bounds + snap MVP)  
- Modifiers / shift-layers  
- Parallel engine evaluation workstreams  

---

## Features wrongly treated as optional (actually core)

| Feature | Why core |
|---------|----------|
| Groups + composite actions | GDD Control Programming pillar |
| Fast Design↔Configure↔Test | Fast Iteration pillar |
| Server blueprint **recompute/validate** | Fair PvP / anti-cheat |
| Frozen MVP **win condition** | Battle/Results impossible otherwise |
| Mass + CoM feedback | Engineering pillar |
| Power (static budget at minimum) | Design constraint in GDD |
| Functional part disable (not whole-robot HP only) | Differentiator vs arcade robot games |
| Shared ActionCommand path in net spike | Otherwise STAGE 2 false GO |
| Sync/cost budgets in construction validator | Prevents un-networkable robots |
| Authoritative sim host (non-negotiable) | Multiplayer-first |

---

## THINGS WE SHOULD NOT BUILD YET

1. Career / economy / paid power  
2. Large component catalog  
3. Ranked MM / many playlists  
4. Full prediction+reconcile stack  
5. Production detach/debris  
6. Replay scrubber / spectator product  
7. Second physics engine / multi-engine ports  
8. Custom visual scripting for controls  
9. Host-migration / P2P production topology  
10. Deep material simulation (heat, fatigue)  
11. Cinematic PvE campaign  
12. Cosmetics pipeline (until core loop ships)  
13. Cloud inventory  
14. Over-generic capability/ECS framework without 4 concrete parts  
15. «Preview PhysX» separate from battle PhysX  

---

## Contradiction index (short)

| Pair | Issue |
|------|-------|
| GDD ↔ SDS | Engine lock vs evaluation reopen (H1) |
| GDD ↔ SDS | Materials durability MVP vs thin SDS model (M2) |
| GDD ↔ Roadmap | «Fragile MP OK» vs hard-stop (M10) |
| GDD ↔ Roadmap/SDS | Control Programming core vs late composite net proof (C1/H3) |
| Roadmap ↔ SDS | STAGE 2 GO finality vs later topology changes (C2) |
| GDD fantasy ↔ SDS detach optional | Destruction language vs net reality (H7) |
| SDS preview approx ↔ GDD physics-truth loop | Dual pipeline (H2) |

---

## Hidden dependency map (highest leverage)

```
Construction freedom
    → body/joint count
        → server phys cost + snapshot size
            → STAGE 2 playability
                → whether free construction is even viable

Damage detach
    → mid-match topology change
        → invalidates earlier sync GO

Control composites
    → command stream shape
        → must exist in STAGE 2 or gate is false

Win condition
    → damage semantics
        → results schema
            → disconnect policy
```

---

## Scores (1–10)

| Dimension | Score | Comment |
|-----------|------:|---------|
| **Gameplay architecture** | **7** | Loop and pillars clear; wincon/power/detach language gaps |
| **Physics architecture** | **5** | Honest about experiments; still assumes networkable detailed joints too readily |
| **Multiplayer architecture** | **6** | Good authority instincts + hybrid hypothesis; STAGE 2 scope too thin on control/damage; prediction footgun |
| **Data architecture** | **7** | Blueprint/runtime split correct; god-blob + schema migration thin; power model missing |
| **Scalability** | **4** | 1v1 minded; weak multi-match story; sync budget not first-class |
| **MVP feasibility** | **5** | Feasible **only if** MVP ruthlessly cut (disable-not-detach, no predict, early composites, re-gates). As written aspiration > envelope |

**Overall stance:** Документы выше среднего для pre-production (риск осознан, gates есть), но **ложное чувство безопасности STAGE 2** — главный системный дефект. Самый опасный сценарий уже назван в focus: *local physics green, PvP sync red after construction/damage complexity lands*.

---

## Top 5 actions before writing more systems

1. Freeze MVP win condition.  
2. Expand STAGE 2 with ActionCommand + composite drive; blueprint-spawn only.  
3. Add re-gates after STAGE 3 and STAGE 7.  
4. Introduce SyncBudget into validation; plan proxy fallback.  
5. Disable-only damage contract for MVP; prediction OFF by default.
