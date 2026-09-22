# Plan — First playable textured build

> Product target beyond thin Stage 11 exit: **a Windows `.exe` that feels like a game**  
> (readable arena + robot materials), while keeping physics+MP invariants.  
> Companion to [`TASK_QUEUE.md`](TASK_QUEUE.md) · [`STAGE11_EXIT.md`](../experiments/STAGE11_EXIT.md) · GDD §17.

**Last update:** 2026-09-22 — textured EXIT PASS; Stage 13 feel PASS; Stage 14 P2 PASS (idle)

---

## 1. Сверка с текущим планом

| Источник | Что говорит | Факт |
|----------|-------------|------|
| `STAGE11_EXIT` | Thin MVP playable **PASS** | OK — Design→Wire→Drive→Fight→Results + UDP/LAN |
| `TASK_QUEUE` open | **S12-T01** soak, **S12-T02** stalemate UX | **Не сделаны** (сессия прервалась на старте S12) |
| Queue status line | «Idle until Stage 12 opened» | **Устарело** — S12 уже в Open |
| GDD §17 MVP | Играбельный RA2-like продукт | Системы есть; **визуал = procedural solids**, не textures |
| GDD chassis | Paint/custom textures = **post-MVP** | Ок для GDD; ваш бар «с текстурами» = **отдельный presentation milestone** |
| Project rules | No premature content catalog / economy | Держим: textures ≠ catalog |

**Итог сверки:** системный thin MVP закрыт. Следующий roadmap-шаг — Stage 12 hardening. Параллельно (или сразу после короткого S12) нужен **явный presentation track** до «играбельного с текстурами», иначе очередь останется в soak/UX и не дойдёт до art.

---

## 2. Определение цели (договориться)

**«Первый играбельный билд с текстурами»** = один `.exe`, в котором игрок:

1. Собирает/грузит бота, вайрит, Test Room, бьётся 1v1 (local + хотя бы UDP loopback).  
2. Видит **не серые примитивы**: пол/борта арены + корпус/колёса/оружие с URP Lit materials + albedo (или atlas).  
3. Понимает исход боя (immobility / timeout) без debug-текста.  
4. Не ломает Stage 2 authority / smoke.

**Не входит в этот бар:** paint shop UI, full parts catalog, career, ranked, detach debris, NGO freeze.

---

## 3. Два трека (порядок)

```
Track H — Hardening (Stage 12 thin)     Track V — Visual / playable feel
S12-T01 soak UDP+LAN                    V1 material kit (URP)
S12-T02 timeout/stalemate UX            V2 arena textured kit
        \                               V3 robot part textures + tint by armor
         \____ merge → PLAYABLE_TEXTURED_BUILD ____/
```

**Рекомендуемый порядок исполнения:** H1 → H2 → V1 → V2 → V3 → **signed build** → потом content/UX depth.

Почему H перед V: дёшево, уже в очереди, снижает риск «красиво, но flaky net».  
Почему V сразу после: без art билд остаётся tech demo.

---

## 4. Фазы до цели

### Phase H1 — S12-T01 Soak / net smoke
- `-ra2-mvp-soak` или script: N× UDP + N× LAN same-process.  
- Pass = zero critical fail, winners consistent host/client.  
- Evidence: `S12-01_SOAK.md`.

### Phase H2 — S12-T02 Stalemate UX
- Отдельный `MatchWinReason` или явный timeout label (не маскировать под Immobilized).  
- Results title + fight HUD: «TIME / STALEMATE · center rule».  
- Evidence: `S12-02_STALEMATE_UX.md`.

### Phase V1 — Material kit (no catalog)
- `Resources/Mvp/Materials/` (или Addressables later — **не сейчас**): Floor, Fence, Hazard, Metal, Rubber, Accent.  
- URP Lit; shared atlas optional.  
- Assembler / arena dressing apply materials instead of runtime `new Material` random colors.  
- Evidence: `S12-03` or `V1_MATERIAL_KIT.md`.

### Phase V2 — Arena looking like a pit
- Textured floor + fence boards + hazard stripe + simple sky/backdrop.  
- Keep physics colliders unchanged.  
- Camera exposure / one key light bake-friendly.  
- Evidence: `V2_ARENA_TEXTURES.md`.

### Phase V3 — Robot reads as a machine
- Chassis mesh/extrude uses armor-tint material; wheels rubber; motor metal; weapon accent.  
- Still data-driven defs — **texture refs on defs**, not hardcode in MB.  
- Optional: simple decal / team color (blue you / red foe) already partly exists.  
- Evidence: `V3_ROBOT_TEXTURES.md`.

### Phase G — Gate: Playable Textured Build
- Rebuild `Ra2MvpPlayer.exe`.  
- Smoke green + human 10-min playtest checklist (drive, wire flip, local fight, UDP, save/load).  
- Doc: `PLAYABLE_TEXTURED_EXIT.md` (same style as STAGE11_EXIT).

---

## 5. После цели (очередь)

| Priority | Item | Status |
|----------|------|--------|
| P1 | Freehand chassis gizmo | **DONE** S13-T02 |
| P1 | Immobility countdown UI (numbers) | **DONE** S13-T01 |
| P2 | Minimal starter part set textures (spin/battery/board) | **DONE** S14-T01 |
| P2 | Burst/Fire wiring UI path | **DONE** S14-T02 |
| P3 | Dedicated server module | Residual |
| Later | Paint shop, ranked, economy | Explicit backlog |

---

## 6. Жёсткие запреты (до textured gate)

- Economy / career / battle pass.  
- Large parts catalog «ради скриншота».  
- NGO/NFE freeze without EXP.  
- Animation-only fake drive.  
- Second physics path for pretty arena.  
- Gold-plate freehand wire editor before V3.

---

## 7. Решения человека (нужны 1–2 ответа)

1. **Textures source:** procedural/checkered generated in Editor vs hand-painted PNGs in repo? (Default recommendation: **small hand atlas + solid URP Lit**, ~5–8 textures.)  
2. **Scope of “robot textures”:** only chassis+wheels+1 weapon, or every spawned module? (Default: **chassis, wheel, motor, board, weapon** — five.)  
3. **Art after or before soak?** (Default: **H1→H2 then V** as above.)

---

## 8. Executable queue mapping (proposed)

| ID | Task | Status |
|----|------|--------|
| S12-T01 | Soak / net stability smoke | open (next to implement) |
| S12-T02 | Stalemate / timeout UX | open |
| S12-T03 | URP material kit + apply in arena/assembler | proposed |
| S12-T04 | Arena textured pass | proposed |
| S12-T05 | Robot part texture refs (thin set) | proposed |
| S12-T06 | Playable textured exit + rebuild | proposed |

When human confirms §7, promote S12-T03…06 into `TASK_QUEUE.md` Open.
