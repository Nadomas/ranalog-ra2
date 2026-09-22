# ORIGINAL AS-IS — Robot Arena 2

> **Назначение:** расшифровка оригинальной установки RA2 как **базы дизайна** для `ra2-analog`.  
> Это **не** копипаст ТЗ и **не** обязательство 1:1. Это карта «как устроено у референса», с уровнем уверенности.  
> Источник: `docs/original/Robot Arena 2/` (локальная копия инсталла).

**Дата скана:** 2026-08-24  
**Версия референса:** Release 1.0 (ReadMe); `.bot` format marker `1.12`

---

## 0. Методика и уверенность

| Уровень | Значение |
|--------|----------|
| **HIGH** | Явно в Tutorials / ReadMe / читаемых `.py` / UI-строках exe |
| **MED** | Сильные косвенные доказательства (RTTI/strings exe, API AI, имена классов) |
| **LOW** | Гипотеза; бинарь / `.cfz` / `.bot` payload ещё не полностью разобраны |

**Что сделано:** инвентарь дерева, Tutorials, ReadMe, `ra2.cfg`, AI/Arenas Python, exe strings/RTTI, `.bot` headers + partial binary markers, **распаковка `Components.cfz` (CFL3/zlib)** → text defs, deep RE dump.  
**Что не сделано:** полная endian/schema `.bot`, closed-form формула урона, точные кг весовых классов, полный layout DirectPlay sync.

---

## 1. Инвентарь установки

### 1.1 Корень

| Путь / файл | Наблюдение | Conf |
|-------------|------------|------|
| `Robot Arena 2.exe` (~3.0 MB) | Основной бинарь + UI + симуляция | HIGH |
| `python22.dll` | Встроенный Python **2.2** для AI/арен/скриптов | HIGH |
| `RA2.cfz` / `Components.cfz` | Архив **`CFL3`** + zlib entries; defs **в cfz**, meshes в `Components/*` | HIGH |
| `ra2.cfg` | Графика + `UpdateInterval=0.05` (50 ms) | HIGH |
| `Events.txt` / `Events_2Player.txt` | Карьера/ивенты (текст + разметка) | HIGH |
| `ReadMe.txt` | Damage, MP TCP/IP, perf notes | HIGH |
| `AI/` | Python AI + команды `.bot` | HIGH |
| `Arenas/` | Арены `.py` + `.gmf` + hazards | HIGH |
| `Components/` | ~**68** папок деталей (mesh `.gmf` + preview) | HIGH |
| `Robot Designs/` | Extra `.bot` + bitmap-текстуры | HIGH |
| `Tutorials/*.htm` | Основной дизайн-док по workshop | HIGH |
| `UI/`, `Sounds/`, `Music/`, `Teams/` | Презентация / мета | HIGH |
| GameSpy URL / era online | Исторический discovery; не модель для нас | HIGH |

**Объёмы (скан):** ~58 `.bot`, ~197 `.gmf`, 68 component folders.

### 1.2 Типы ассетов

| Расширение | Роль | Conf |
|------------|------|------|
| `.gmf` | 3D mesh / arena / component geometry | HIGH |
| `.bot` | Сохранённый робот (text header + binary blob) | HIGH |
| `.cfz` / `CFL3` | Packed text defs (`name=`, `base=`, power/air/elec…) + blobs | HIGH |
| `.gib` | UI layout/chrome | MED |
| `.py` | AI, arenas, practice UI glue | HIGH |
| `.htm` | Tutorials | HIGH |

---

## 2. Технологический стек (AS-IS)

| Слой | Факт | Conf |
|------|------|------|
| Язык клиента | C++ (RTTI: `Motor`, `Piston`, `Armor`, `Battery`, `SmartZone`, …) | HIGH |
| Скрипты | Python 2.2 + модуль `plus` (C++ ↔ Python) | HIGH |
| Физика | **Havok** (RTTI `Havok::*`, `GMID_HAVOK_*`) | HIGH |
| Constraints | Hinge, Prismatic, Wheel, Point-to-point, Dashpots, Limited PtP | HIGH |
| Сеть | TCP/IP + **DirectPlay** + GameSpy master; host sync messages | HIGH |
| Input | Keyboard / mouse / gamepad (optional) | HIGH |
| Tick / sync | `UpdateInterval=0.05` (gfx/net cfg); AI rethink ≈ **8 Hz** (`tick`×8 ≈ 1s) — MED | HIGH/MED |

**Вывод для аналога:** референс — **не** «анимация боя», а **constraint/rigid-body** симуляция (Havok hinge/prismatic/wheel). Наш Unity PhysX/Articulation путь концептуально ближе, чем kinematic fakes.

---

## 3. Игрок и петля дизайна (Workshop)

Поток из Basic Tutorial + UI strings — **HIGH**:

```
Overview
  → Chassis (Structure Design → Armor)
  → Components (attach parts)
  → Wiring (controller + bind inputs → component channels)
  → Paint Shop (optional)
  → Test Robot (garage / barrels / blocks)
  → Snapshot / name
```

### 3.1 DESIGN ≠ CONFIGURE (доказательство из оригинала)

1. **Сначала** строится физическая конструкция (chassis + components + attachment legality).  
2. **Отдельно** строится **controller**: Switch / Button / Analog на сетке слотов.  
3. Затем **wiring**: выбранный control → клик по компоненту → выбор **канала** (CW/CCW, Fire, Extend/Retract, drive axes).  
4. Без **Control Board** wiring недоступен.  
5. Один и тот же набор деталей допускает разные схемы управления (tank steer 2/4 wheel — Mobility Tutorial).

Это прямой референс для нашего pillar **CONTROL PROGRAMMING**.

### 3.2 Chassis

| Правило | Деталь | Conf |
|---------|--------|------|
| Baseplate outline | Top-down точки на grid; max **16** точек | HIGH |
| Замыкание | Последняя точка → первая; без пересечений | HIGH |
| Мин. расстояние | Точки не ближе 1 grid unit | HIGH |
| Step 2 | Extrude; editable top outline → клинья/скосы; высота слайдером | HIGH |
| Freeze | После компонентов форма chassis **не** меняется (сброс деталей) | HIGH |
| Placement | Baseplate components **внутри** chassis; invalid = red | HIGH |
| Axles / shafts | Тело мотора/поршня не сквозь стену; ось/шток — можно | HIGH |
| Wheels | Крепятся **только к axles**, не к baseplate | HIGH |

### 3.3 Armor

Четыре типа (**HIGH**): **Polymer/Plastic, Aluminum, Titanium, Steel** — tradeoff strength ↔ weight / speed / weight class.  
Exe refs: `chassis_alum/plast/steel/tit.txt` (**HIGH**).  
Опция «default chassis appearance» vs ручная покраска.  
Точные числа strength/mass — в packed chassis/armor defs (**MED** после cfz; числа не выписаны сюда полностью).

### 3.4 Weight classes

Exe/UI: **LIGHTWEIGHT / MEDIUMWEIGHT / HEAVYWEIGHT** (**HIGH**).  
`.bot` header `Class: 0|1|2` → с высокой вероятностью LW/MW/HW (**MED**).  
ReadMe: есть max weight; «много moving parts» бьёт perf независимо от веса. Точные кг — **LOW**.

---

## 4. Компоненты (таксономия)

Категории UI (**HIGH**):

| Категория | Примеры из инсталла |
|-----------|---------------------|
| **Power** | `controlboard`, `battery1/2`, `battery_slimpack`, `airtank`, `airtank_big` |
| **Extenders** | `extender_round`, `extender_roundB`, `extender_square`, brackets |
| **Mechanics** | spin (`ztek`, `redbird`, …), `burstmotor`, `servo_motor`, `burstpiston`, `servopiston`, `linear_actuator`, `anglemotor`, `carsteering`, `axle`, … |
| **Treads** | `wheel1..3`, `miniwheel`, `mudtire`, `shinywheel`, `caster`, … |
| **Weapons** | axe/hammer/blade/spike/plow/disc/… |
| **Extras** | `ballast`, wedges, `smartzone`, `anchor`, … |

~68 discrete component **mesh** folders; **~131 text defs** внутри `Components.cfz` (styles/variants). Каждый loose folder: `*.gmf` + `_preview.bmp`. Bot ссылается на `Components\*.txt` из архива.

### 4.0 Def schema (`Components.cfz`) — HIGH

Key=value text, примеры полей:

| Field | Role |
|-------|------|
| `name`, `dir`, `model`, `preview`, `description` | Identity / assets |
| `type` | UI category: power / mechanics / mobility / weapons / extenders / extras |
| `base` | Runtime class: `SpinMotor`, `BurstMotor`, `ServoMotor`, `BurstPiston`, `ServoPiston`, `Wheel`, `Battery`, `AirTank`, `ControlBoard`, `Weapon`, `SmartZone`, `Steering`, `AxleMount`, `Component` |
| `power = a b` | Motor/piston strength pair |
| `elecMaxInOutRate`, `electotal` | Electric draw / battery capacity |
| `airtotal`, `airmaxinoutrate` | Pneumatic capacity / rate |
| `burst = …` | Burst cock/fire angles |
| `concussion`, `piercing` | Weapon damage mix (0–1) |
| `hitpoints`, `fracture`, `mass` | Structure / break (на части деталей) |
| `grip`, `resistance`, `contact` | Wheel traction |
| `passthru`, `attaching`, `master` | Collision / attach nodes |
| `damageable = false` | e.g. Control Board, anchors |
| `styles = …` | Size/angle variants → other `.txt` |

**Примеры (verified extract):**

- AirTank: `airtotal = 800` / `2000`; rates 120–200  
- Battery «Nifty 6V»: `electotal = 24000`, `elecmaxinoutrate = 400`  
- SpinMotor (angle): `power = 18 14`, `elecMaxInOutRate = -80`  
- BurstMotor DDT: `burst = 0 50 90`, `power = 5 35`, `elecMaxInOutRate = -500`  
- Weapon axe: `concussion = .9`, `piercing = .4`  
- ControlBoard: required; `damageable = false`

### 4.1 Моторы (Mechanics Tutorial) — HIGH

| Тип | Поведение | Wiring |
|-----|-----------|--------|
| **Spin** | Свободное вращение, CW/CCW, нет «дома» | Analog или Button → CW или CCW |
| **Burst** | Взвод → триггер → мощный частичный дуг (&lt;180°) | Button → **Fire** |
| **Servo** | Медленное управляемое вращение; **lock** при остановке | Analog bi-dir |

### 4.2 Поршни — HIGH

| Тип | Поведение | Питание | Wiring |
|-----|-----------|---------|--------|
| **Burst piston** | Резкий вынос → медленный возврат | **Air tank** (не battery) | Button → **Fire** |
| **Servo piston** / Linear Actuator | Extend/retract, lock mid-stroke | Air tank | Analog → extend / retract |

Правило: motors → **battery**; pistons → **air tank**; гибрид → оба.

**Dual resource model (HIGH):** electricity (`electotal` / rates) + compressed air (`airtotal` / rates). Damage makes internals «less effective» (ReadMe), не hard-off.

### 4.3 Mobility (как строится езда)

- Типичный **tank / differential steer**: два Analog — Forward/Back и Left/Right, разведённые по колёсным spin-моторам (**HIGH**, Mobility Tutorial).  
- 2-wheel vs 4-wheel drive/steer — вариации той же wiring-схемы.  
- **Car steering** component + `bCarSteering` в AI (**MED/HIGH**).  
- AI drive API: `Input('Forward', …)`, `Input('LeftRight', 0/1, …)` — именованные оси контроллера, не прямые joint writes из скрипта тактики (**HIGH**).

### 4.4 SmartZone

Компонент-сенсор (**HIGH** в AI): зона контакта; события `SmartZoneEvent(direction, id, robot, chassis)`; различение «часть бота» vs «chassis in zone». Используется оружейным AI (триггеры по зоне).

---

## 5. Управление (Controller model)

Три примитива (**HIGH**):

| Control | Семантика |
|---------|-----------|
| **Switch** | Toggle on/off |
| **Button** | Пока зажато / one-shot Fire |
| **Analog** | ± power; клавиатура ≈ ±100%; стик — partial |

Связь: **named control slot** → **component channel**.  
Много контролов на один мотор и наоборот — норма.

**Implication для ra2-analog:** сохранить слой  
`device → named signals → binding graph → actuator commands`  
отдельно от `blueprint / assembly`.

---

## 6. Физика и симуляция

### 6.1 Backend

**Havok rigid bodies + constraints** (**HIGH**).  
Арены управляют hazards через Python wrappers:

- `prismatic.ApplyForce(power)`, `SetDirection`, `Lock`, `SetPowerSettings`  
- `hinge.SetDirection`, `Lock`, `SetAutoLocks`  

→ игровые «пистоны/шарниры арены» = те же классы constraint API, что и у ботов (**MED**).

### 6.2 Что чувствует игрок (дизайн-интент)

- Масса / CoM / armor weight влияют на скорость и класс.  
- Движущиеся части дороги по CPU (ReadMe).  
- Chassis deformation визуально опциональна (detail settings) — есть концепт деформируемого корпуса (**HIGH** как feature flag, **LOW** как формула).

### 6.3 Tick

`UpdateInterval=0.05` → **20 Hz** update preference в конфиге игрока (**HIGH** как setting; не доказано, что physics substep = ровно это).

---

## 7. Урон, выход из строя, победа

### 7.1 Internal damage — HIGH (ReadMe)

> Hits to the **chassis** damage surrounding components by **distance from hit**.  
> Motors, batteries, etc. become **less effective**; smoke/FX; **не** полный hard-off.

### 7.1b Weapon scalars — HIGH (defs)

Оружие несёт `concussion` + `piercing` (axe 0.9/0.4; ice pick-style high pierce). Decals + `normal` для направления удара. Closed-form impact×armor — **LOW**.

Scripted hazard APIs (**HIGH**): `plus.damage(bot, component, amount, pos)`, `plus.force`, `plus.zap`, `plus.eliminatePlayer`, KOTH `plus.addPoints`.

### 7.2 Immobility — HIGH

- UI/sounds: immobile countdown, `IMMOBILIZED`.  
- AI: `ImmobilityWarning(id, on)`; тактики реагируют на `bImmobile`.  
→ Победа/поражение завязаны на **неспособность двигаться**, не только на HP bar.

### 7.3 Fracture — MED

`FractureEvent` в AI → refresh SmartZone sensors (структурный сбой / отрыв влияет на сенсоры). Точная механика detach — **LOW**.

### 7.4 Damage API (AI) — MED

`GetLastDamageDone` / `GetLastDamageReceived`, callbacks; `GetComponentHealth(i)`.

---

## 8. Режимы и арены

### 8.1 Game types — HIGH

Из AI + exe: **DEATHMATCH**, **BATTLE ROYAL**, **TEAM MATCH**, **TABLETOP**, **KING OF THE HILL** (+ exhibition / practice).

- **TABLETOP** — edge avoidance / push-off-edge tactics.  
- **KING OF THE HILL** — dethrone / reign; zone scoring `addPoints`.  
- Pits: eliminate on low `y`.

### 8.2 Арены (папки)

`practice arena`, `tabletop`, `kingofhill`, `compressor`, `electric`, `flextop`, `octagon`, `parkinglot`, `skull`, `bridge`, `hilltop`, `half`, `box`, … + obstacle packs (`crates`, `barrels`, `cones`, `ramps`, `cinderblocks`).

Hazards: spikes (prismatic force), saws, hellraiser (hinge), и др. в `Arenas/Hazards.py`.

Practice: смена obstacle через UI checkbox → `AddXtra` GMF.

---

## 9. AI-архитектура (как устроена «мозг»-надстройка)

```
Bindings.py  →  (bot name → AI class + params)
SuperAI      →  tactics scheduler + drive helpers
Tactics.py   →  Charge / Engage / Invert / Unstuck / KOTH / Tabletop …
Weapon AIs   →  Spinner, Flipper, Chopper, Poker, Rammer, Whipper, Pusher, …
plus (C++)   →  world query, Input(), components, sounds, game type
```

**Параметры биндинга (HIGH):** `nose`, `invertible`, `topspeed` (m/s), `throttle` (analog max), `turnspeed` (rad/s), `turn`, `radius`, `weapons` (component ids), `car`, zone/trigger names.

**Важно:** AI не «чит-телепортит». Он пишет в **те же Input-каналы**, что и игрок (Forward / LeftRight / named weapon triggers). Это эталон для нашего command bus.

---

## 10. Мультиплеер (AS-IS)

| Факт | Conf |
|------|------|
| Transport: TCP/IP; stack markers: **DirectPlay** + GameSpy | HIGH |
| Create Server: «connection speed» = rate of sync updates | HIGH |
| CLI: `-host`, `-connect` | HIGH |
| Message types (exe): `giSyncMessage`, `giTimeSyncMessage`, `giRemoteInputMessage`, `giBotDamageMessage`, `giBotImmobileMessage`, `giMatchWinMessage`, `giAddPointsMessage`, … | HIGH |
| Sync errors: «Bad Rigid Body ID / Controller ID in SyncList» | HIGH |
| Inference: **host-authoritative** sim; clients send remote input; selective body/controller sync — not proven full lockstep | MED |
| Полный packet layout | LOW |

**Для ra2-analog:** наследуем *требования* (server-side sim, input in / state out), не DirectPlay/GameSpy. Наш Stage 2 UDP listen-host — осознанная замена.

---

## 11. Формат `.bot` (частично)

**Text header (ASCII, LF)** — HIGH. Версии в инсталле: `1.07`…`1.12` (большинство `1.12`). Пример:

```
1.12
Name: Scout
Class: 0
1
true
```

Затем binary payload (~330–335 KB).

**Binary markers (HIGH/MED):**

| Marker / pattern | Meaning |
|------------------|---------|
| `Chassis` + polygon floats | Baseplate/top outline, height |
| `ControlBoard` → `Components\controlboard.txt` | Part instance + def path |
| `SpinMotor` / `BurstMotor` / `Wheel` / `Battery` / `Weapon` / `SmartZone` / … | Typed instances |
| Quaternions / positions / joint angles | Placement |
| `Name: Forward`, `Name: LeftRight`, `Name: Smash` | Controller slots |
| Wiring triples e.g. `1 0 1` | Control↔device/channel indices (**MED**) |
| SmartZone string e.g. `weapon` | Matches AI `RegisterSmartZone` |

Поля header **MED**: `Class` = weight class; trailing `1`/`true` = flags (opaque).

**Полная schema** (endian, paint atlas, wiring ID tables) — **не** закрыта. Для аналога: `ra2.robot_blueprint.v1` (chassis + parts + control/wiring + power) — см. S3-04; v0 legacy placement-only.

---

## 12. Архив `CFL3` (`.cfz`)

Magic: `CFL3` + per-entry **zlib** (`78 9C`).  
`Components.cfz` → text component defs (см. §4.0).  
`RA2.cfz` → крупные blobs (UI/textures/прочее; не все типизированы).

Распаковка defs: **сделана** (выборочно). Полный dump всех 131+ entries в репо — опциональный follow-up.

---

## 13. Карта наследования → `ra2-analog`

### 13.1 Обязательно сохранить (духовный / системный core)

1. **Свободная конструкция** chassis + modular parts + attachment rules.  
2. **DESIGN ≠ CONFIGURE** — отдельный wiring/controller слой.  
3. **Физические actuators** (spin / burst / servo × rotary & linear), не «анимация атаки».  
4. **Power split** battery (elec) vs pneumatic air — с бюджетами `electotal` / `airtotal`.  
5. **Урон через конструкцию** (chassis splash + concussion/piercing) и degradation.  
6. **Immobility / eliminate** как win paths (+ KOTH/tabletop).  
7. **Test garage** быстрый цикл.  
8. **Data-driven components** (у них cfz text defs; у нас JSON/SO).  
9. **Commands → actuators**, не UI→joint.  
10. **Host-auth sync** + remote input (идея; не их протокол).

### 13.2 Осознанно не копировать 1:1

| Оригинал | Наш путь |
|----------|----------|
| Havok | Unity physics (доказанный Stage 1–2) |
| Python AI в рантайме боя | позже; сначала human + thin net |
| TCP + GameSpy | UDP listen-host / будущий DS |
| 16-point chassis only | можно расширить, но правила валидации — в духе RA2 |
| CFL3 / `.bot` binary | свой blueprint schema |
| Career Events text grind | не приоритет до physics+MP proof |

### 13.3 Уже закрыто у нас vs пробелы относительно оригинала

| Оригинал | ra2-analog сейчас (2026-09-22) |
|----------|--------------------------------|
| Chassis editor ≤16 | **PASS** thin (polygon + freehand gizmo) |
| DESIGN ≠ CONFIGURE + Fire | **PASS** thin (Drive/Turn/Fire + Wire Fire) |
| Spin/burst/servo + pistons | **PASS** thin Stage 7+ |
| SmartZone | thin spike exists (S7-09) |
| Immobility countdown | **PASS** (S8 + S13 HUD) |
| Modular assemble + blueprint JSON | **PASS** Stage 3+ |
| Net authority | **PASS** Stage 2 GO |
| Practice obstacles | **PASS** S16-T01 |
| Armor 4-type workshop cycle | **PASS** S16-T02 (mass tradeoff thin) |
| Paint shop | Later / post-MVP |
| Full ~68 catalog | Explicit backlog |

---

## 14. Открытые gaps

1. Полный dump всех `.txt` из `Components.cfz` (+ chassis armor) в `docs/original/extracted/` — по желанию.  
2. Endian/schema binary `.bot` (wiring ID tables, paint).  
3. Closed-form damage: impact × concussion/piercing × armor.  
4. Точные кг weight class.  
5. Net: полный SyncList layout (только если понадобится; не цель совместимости).  
6. Attachment rule matrix per `base` type.  
7. Точное соотношение `UpdateInterval` vs AI `tickInterval`.

---

## 15. Краткая «одностраничная» модель оригинала

```
Chassis(outline≤16, height, armor) 
  + Parts(power elec|air, mechanics, treads, weapons, extras)
  + Attachment graph (axle/shaft/baseplate rules)
  + Controller(Switch|Button|Analog slots)
  + Wiring(signal → component channel)
  → Runtime: Havok RB + Hinge/Prismatic/Wheel + dual energy budgets
  → Damage: chassis splash + weapon concussion/piercing → component HP/fracture
  → Outcome: immobilize / eliminate / KOTH points / tabletop edge
```

**Эмоциональный core:** игрок собирает *машину* и *схему управления*, затем проверяет гипотезу в физике против другой такой же системы.

---

## 16. Ссылки на исходники скана

- `docs/original/Robot Arena 2/Tutorials/Basic_Tutorial.htm`  
- `…/Mobility_Tutorial.htm`  
- `…/Mechanics_Tutorial.htm`  
- `…/ReadMe.txt`  
- `…/ra2.cfg`  
- `…/AI/__init__.py`, `Bindings.py`, `Tactics.py`  
- `…/Arenas/Hazards.py`, `Scripts/Practice.py`  
- `…/Components/*`, `…/AI/Team*/Bot*.bot`

Связанный проектный контекст: `docs/PROJECT_CONTEXT.md`.
