# S1-03 — Wheel / Joint Spike Plan

> **Статус:** реализовано — **PASS** (10/10). Отчёт: [S1-03-wheel-joint-spike.md](./S1-03-wheel-joint-spike.md).  
> **Этап:** STAGE 1 — Physics Prototype (локально).  
> **Связь:** EXP-01 (thin), EXP-02 (счётчик bodies/joints), EXP-03 (local, контакт с Robot_B).  
> **База:** сцена `PhysicsTest` + S1-01 smoke / S1-02 drive (chassis + primitive wheels, `FreezeRotationX|Z`).

---

## 1. Цель эксперимента

Доказать на **одном** колесе одного робота (`Robot_A`), что связка **отдельный wheel Rigidbody + joint к chassis** в текущем PhysX/PhysicsTest:

- не взрывает симуляцию;
- не даёт систематический jitter / penetration;
- сохраняет читаемый drive и контакт (стена, Robot_B);

и тем самым даёт **локальное** основание позже снимать хак `FreezeRotation` и наращивать настоящую колёсную/суспензионную модель — **без** фиксации финального physics stack и **без** multiplayer.

Гипотеза: минимальный hinge-like joint на одном колесе достаточно стабилен на demo-сборке PhysicsTest, чтобы считать joint-attachment жизнеспособным следующим шагом STAGE 1 (а не тупиком «только FreezeRotation + compound colliders»).

---

## 2. Одна рекомендуемая минимальная реализация

### Рекомендация первого spike: `HingeJoint`

| Элемент | Решение |
|---------|---------|
| **Joint** | `HingeJoint` (первый кандидат; `ConfigurableJoint` — запасной follow-up, не в scope S1-03) |
| **Где живёт joint** | На объекте **одного** колеса (предпочтительно `Wheel_FL` у `Robot_A`) |
| **Connected body** | Rigidbody корпуса (`Robot_A` root) |
| **Ось** | Ось вращения колеса (локальная ось цилиндра после `Euler(0,0,90)` в builder — типично ось вдоль «оси диска», т.е. поперечная оси шасси) |

### Chassis / wheel / connection

| Роль | Объект | Содержимое |
|------|--------|------------|
| **Chassis** | `Robot_A` (root) | Существующий `Rigidbody` (mass ~12), compound colliders: `Chassis` + три остальных колеса + без коллайдера у `Nose`. Drive S1-02 (`PhysicsTestDrive` → силы/момент на chassis RB) **не менять по смыслу**. |
| **Wheel (spike)** | `Wheel_FL` | Отвязать от compound: собственный `Rigidbody` + существующий `SphereCollider` (+ mesh). Не кинематический. |
| **Connection** | `HingeJoint` на `Wheel_FL` | `connectedBody` = chassis RB; anchor/axis выровнены под ось колеса; без motor drive в S1-03 (вращаемость от контакта/инерции достаточна для spike). |

### Какие Rigidbody нужны

1. **Chassis RB** — уже есть на root `Robot_A` (и пассивный `Robot_B` без изменений joint-spike).  
2. **Один wheel RB** — только `Wheel_FL` на `Robot_A`.  
3. **Не добавлять** RB на остальные колёса Robot_A и ни на одно колесо Robot_B в S1-03.

`FreezeRotationX | FreezeRotationZ` на chassis **оставить** на время spike: цель — стабильность joint, а не одновременный отказ от upright-hack. Снятие freeze — отдельный follow-up после успеха S1-03.

---

## 3. Почему этот вариант подходит для первого spike и не является финальной архитектурой

- **Минимальный DOF:** `HingeJoint` = одна ось вращения; меньше ручек, чем у `ConfigurableJoint` (linear/angular locks, spring/damper наборы).  
- **Совпадает с текущей геометрией:** в builder колёса — примитивы-цилиндры с sphere collider; ось «как у колеса» естественно мапится на hinge.  
- **Один joint / один лишний body:** тонкий probe в духе **EXP-01 thin** (не full blueprint runtime) и даёт первые числа для **EXP-02** (bodies/joints на машину) без stress-лестницы.  
- **Контакт с Robot_B** остаётся локальным smoke в духе **EXP-03 local** — без net, без detach.  
- **Не финальная архитектура:** не выбираем WheelCollider vs raycast vs full suspension; не фиксируем PhysX vs alt; не вводим packages; не проектируем modular connection language / detach. `ConfigurableJoint` и полная 4-wheel + suspension остаются открытыми после отчёта.

---

## 4. Что измеряем

| Метрика | Как смотреть (локально, вручную / лог) |
|---------|----------------------------------------|
| **Stability** | Нет NaN/Inf позиций/скоростей; нет «взрыва» (скорости не уходят в нечитаемый разгон за секунды покоя/мягкого drive). |
| **Jitter** | Визуально и по семплам позиции/угла колеса и chassis: нет высокочастотной дрожи на месте и на ровном полу при постоянном drive. |
| **Penetration** | Нет устойчивого пересечения колеса с полом/корпусом; краткие контакты допустимы, постоянный tunneling — нет. |
| **Impact behavior** | Удар о стену и таран Robot_B: импульс читаем, роботы не телепортируются, joint не «отстреливает» колесо сквозь геометрию. |
| **Body / joint counts** | Зафиксировать на Robot_A после setup: `Rigidbody` count (ожидание: chassis + 1 wheel = **2** на A; B без изменений) и `Joint` count (**1** `HingeJoint`). Записать как черновую точку для EXP-02 budget later. |

Опционально (не блокирует): краткий note по ощущению physics step (есть ли overt overrun на demo) — без формального profiler gate STAGE 1.

---

## 5. Сценарии тестов

Все — **локально**, сцена PhysicsTest, управление S1-02 на Robot_A где уместно. Robot_B — пассивный obstacle (как в S1-02 drive scenario), если не указано иное.

1. **Drive** — прямолинейный ход вперёд/назад ~5–10 с; колесо с joint должно оставаться прикреплённым, chassis едет предсказуемо.  
2. **Turn** — поворот на месте / в движении (A/D + W); нет срыва joint и нет нарастающего jitter.  
3. **Wall hit** — разгон в стену арены; контакт читаем, нет explosion / NaN.  
4. **Collision with Robot_B** — наезд/таран пассивного Robot_B; читаемый контакт (EXP-03 local thin).  
5. **Ten repeated runs** — один и тот же протокол (spawn → drive → turn → wall → B) × **10** Play Mode запусков; сравнить qualitatively + отмечать fail/pass по критериям §6.

---

## 6. Критерии успеха и провала

### Успех (S1-03 pass)

- ≥ **9/10** прогонов без simulation explosion, NaN/Inf и без отрыва колеса от шарнира.  
- Drive + turn остаются управляемыми на уровне S1-02 (силы на chassis).  
- Wall hit и контакт с Robot_B **читаемы** (playtester может объяснить исход).  
- Нет **систематического** jitter/penetration на полу.  
- Зафиксированы counts: **2** RB на Robot_A (chassis+wheel), **1** joint; Robot_B без joint-spike.

### Провал (S1-03 fail → redo / alternate)

- Систематический explosion, NaN, или joint «ломает» машину в большинстве прогонов.  
- Постоянный jitter/penetration, делающий drive нечитаемым.  
- Удары о стену / Robot_B регулярно дают teleport / нефизичный разлёт.  
- Стабильность достигается только «магическими» нефизичными костылями (телепорт, kinematic каждый кадр, ручной `transform` на dynamic) — это **не** pass.

**При fail:** сначала redo настройки (mass колеса, anchor/axis, solver iteration, damping) на том же `HingeJoint`. Если hinge принципиально нестабилен на этой геометрии — follow-up spike с `ConfigurableJoint` (locked to hinge-like), **не** смена engine/packages.

---

## 7. Риски и открытые вопросы

| Риск / вопрос | Комментарий |
|---------------|-------------|
| Compound → separate wheel | Остальные колёса всё ещё compound; асимметрия массы/контактов может уводить машину в сторону. Допустимо для spike; не чинить 4 колеса сразу. |
| Mass / inertia колеса | Слишком лёгкое/тяжёлое колесо → jitter или «якорь». Нужен простой стартовый mass (доля от chassis), без «финальной» калибровки. |
| Hinge vs ConfigurableJoint | Hinge может оказаться слишком жёстким/бедным для будущего modular language — это **открыто**; S1-03 не закрывает выбор. |
| FreezeRotation still on | Успех joint ≠ готовность снять freeze. Открытый follow-up: upright без freeze (суспензия / больше constraints). |
| Drive всё ещё на chassis | Мотор на колесе / torque на hinge **не** в S1-03; открыто для S1-0x later. |
| Friction material | `PhysicsTestSlide` очень скользкий — может маскировать или усугублять поведение колеса; не менять материал «для красоты» без записи в отчёт. |
| EXP-02 budget | Один joint не даёт stress-потолка; только baseline count. |
| Packages / stack freeze | **Запрещено** в этом spike: не добавлять packages, не объявлять PhysX финальным. |

---

## 8. Краткий brief на будущую реализацию S1-03 (для Cursor later)

**Scope (только когда попросят реализовать):**

1. Расширить `PhysicsTestSceneBuilder` (или тонкий editor/runtime helper **только** для PhysicsTest): для `Robot_A` / `Wheel_FL` — отдельный `Rigidbody`, снять участие в compound-только модели, повесить `HingeJoint` → chassis RB, выставить axis/anchor.  
2. Остальные колёса A и весь Robot_B — без joint-spike.  
3. Сохранить S1-02 pipeline: `PhysicsTestPlayerInput` → `PhysicsTestDriveCommand` → `PhysicsTestDrive` (силы на chassis); не читать devices из joint-кода.  
4. Оставить `FreezeRotationX|Z` на chassis.  
5. Не трогать Packages / ProjectSettings / net.  
6. Добавить минимальный local-only verifier/лог (по аналогии smoke/drive verifier): counts bodies/joints, NaN flag, qualitative pass notes; прогнать 10 сценариев §5.  
7. После прогонов — отчёт в `docs/experiments/` (например `S1-03-wheel-joint-spike.md` или `EXP-01`-thin note): success/fail, counts, риски, рекомендация «оставить Hinge / пробовать ConfigurableJoint».

**Явно out of scope реализации S1-03:** 4-wheel joints, suspension, WheelCollider package path, motorized hinge, снятие FreezeRotation, multiplayer, финальный physics architecture freeze.

---

*Конец плана S1-03. Реализация кода/сцены — отдельный запрос.*
