# Plan — Playable RA2-analog (post-textured)

> Companion to [`TASK_QUEUE.md`](TASK_QUEUE.md) · [`ORIGINAL_AS_IS.md`](../ORIGINAL_AS_IS.md) · GDD §17 · `docs/original/Robot Arena 2/`.

**Last update:** 2026-09-22 — Stage 16 PASS (Practice obstacles + Armor cycle); queue idle

---

## 1. Сверка с оригиналом (2026-09-22)

| Оригинал (HIGH) | ra2-analog сейчас | Gap |
|-----------------|-------------------|-----|
| Design → Wiring → Paint? → **Test Robot** → Battle | Design / Wire / Drive / Fight | Paint = later |
| Practice garage obstacles (`Practice.py`: crates/blocks/barrels/cones/ramps) | Test Room cycle (procedural) | **DONE** S16-T01 |
| Armor Polymer/Alum/Ti/Steel | Design cycle + chassis mass | **DONE** S16-T02 |
| Controller Switch/Button/Analog + Fire wiring | Thin Drive/Turn/Fire + Wire Fire | OK thin |
| Immobility countdown | HUD + lock pill | OK |
| Chassis ≤16 polygon + freehand | Editor + gizmo | OK thin |
| Textured arena/parts | URP kit + starter mats | OK textured exit |
| Dedicated Server path | Headless player only | P3 residual (module) |
| Career / Events.txt / full ~68 catalog | Explicit backlog | Do not pull |

**Итог:** textured playable bar met. Next RA2-aligned thin work = **Practice obstacles** then **Armor cycle** — not economy, not DS module install.

---

## 2. Цель «играбельный как RA2» (текущий бар)

1. Design → Wire → Test (с препятствиями) → Fight → Results.  
2. Читаемые materials (не серые примитивы).  
3. Immobility / timeout понятны.  
4. Physics+MP Stage 2 invariants green.

**Не входит:** paint shop UI, full catalog, career Events, ranked, detach debris, NGO freeze.

---

## 3. После textured exit (очередь)

| Priority | Item | Status |
|----------|------|--------|
| Done | Textured exit + feel (S12–S13) | PASS |
| Done | Starter mats + Fire UI + default spinner bot (S14–S15) | PASS |
| **P1** | Practice Test Room obstacles (RA2 Practice.py) | **DONE** S16-T01 |
| **P1** | Armor type cycle (4 types → chassis mass) | **DONE** S16-T02 |
| P3 | Dedicated Server Win module | Residual — human install |
| Later | Paint shop, ranked, economy, catalog | Explicit backlog |

---

## 4. Жёсткие запреты

- Economy / career / battle pass.  
- Large parts catalog «ради скриншота».  
- NGO/NFE freeze without EXP.  
- Animation-only fake drive.  
- Second physics path for pretty arena.
