# Unity Engine — ra2-analog

**Статус:** Unity зафиксирован как **production engine**.  
Unreal Engine и прочие движки **не** рассматриваются как production-альтернативы.

Конкретные технические решения *внутри* Unity принимаются только на основе требований проекта и **экспериментальной валидации**, не на предположениях.

Связанные документы: [TECHNICAL_PRINCIPLES](TECHNICAL_PRINCIPLES.md) · [GDD](GDD.md) · [TECHNICAL_ROADMAP](TECHNICAL_ROADMAP.md)

---

## 1. Зафиксировано

| Решение | Статус |
|---------|--------|
| Production engine = **Unity** | Зафиксировано |
| Альтернативные движки для production | Вне scope |
| Выбор packages / physics / netcode / server model | Только после валидации |

### Текущий workspace baseline (не = финальный pin)

В репозитории уже есть проект:

- путь: `UnityProject/ra2-analog/`
- editor: **Unity 6000.5.9f1** (Unity 6)
- render pipeline в manifest: **URP**
- input: **Input System**
- physics module: встроенный `com.unity.modules.physics` (PhysX 3D)
- dedicated networking stack: **ещё не подключён**

Этот baseline — стартовая точка экспериментов. Долгосрочный pin версии и packages — отдельные решения ниже.

---

## 2. Принцип принятия решений

1. Сформулировать требование из GDD / Technical Principles.  
2. Перечислить кандидатов **внутри Unity ecosystem** (и first-party, и проверенные third-party при необходимости).  
3. Если нельзя уверенно выбрать теоретически → **`[EXPERIMENT REQUIRED]`**.  
4. У эксперимента обязаны быть: цель, метод, метрики, критерии успеха/провала, артефакт результата.  
5. Пакеты не добавлять «потому что принято в туториалах».

---

## 3. Области исследования

### 3.1 Версия Unity

| | |
|--|--|
| **Требование** | Стабильный editor/player для physics + multiplayer + long-term maintainability |
| **Текущее** | 6000.5.9f1 в workspace |
| **Кандидаты** | Закрепить текущий Unity 6 stream; перейти на ближайший LTS/поддерживаемый train когда критерии стабильности ясны |
| **Статус** | `[EXPERIMENT REQUIRED]` — pin политики версий |

**Эксперимент U-VER**

- **Цель:** выбрать поддерживаемую версию Unity для команды и CI.  
- **Метод:** поднять пустой/spike-проект на baseline; прогнать smoke physics + net prototype; проверить build Dedicated Server target; зафиксировать известные regressions.  
- **Метрики:** время открытия проекта; успешность player/server build; блокеры packages; частота editor crashes на spike.  
- **Успех:** команда может воспроизводимо работать на одной версии; critical packages совместимы; нет show-stopper bugs для spike #1–#9.  
- **Провал:** нестабильный editor или несовместимость ключевого net/physics package без workaround.

---

### 3.2 Physics stack

| | |
|--|--|
| **Требование** | Modular rigid bodies, joints, wheels/motors, runtime assembly, detach, interaction двух роботов, предсказуемость для net |
| **Кандидаты (Unity)** | Built-in 3D Physics (PhysX); Havok Physics for Unity; DOTS/Unity Physics (+ possibly custom); гибриды |
| **Статус** | `[EXPERIMENT REQUIRED]` |

**Не выбирать** «потому что PhysX по умолчанию» или «потому что ECS быстрее» без метрик на *наших* роботах.

**Эксперимент U-PHY** — см. критические эксперименты §5.2–5.4 (локальный physics gate до сети).

---

### 3.3 Networking solution

| | |
|--|--|
| **Требование** | Server-authoritative battle; sync состояния робота/компонентов/урона; input; latency handling; путь к dedicated server |
| **Кандидаты (ориентиры)** | Netcode for GameObjects (NGO); Netcode for Entities (NFE); иные Unity-compatible стеки при доказанной необходимости |
| **Статус** | `[EXPERIMENT REQUIRED]` |

NGO проще для GameObject/PhysX workflow; NFE сильнее для масштабной симуляции, но дороже по сложности. Для ra2-analog решающий фактор — **качество sync физики модульных роботов**, не маркетинговые слайды.

**Эксперимент U-NET** — см. §5.5–5.8.

---

### 3.4 Dedicated server approach

| | |
|--|--|
| **Требование** | Headless/server build, authoritative simulation, приемлемый CPU cost на матч |
| **Кандидаты** | Unity Dedicated Server package + build target; отдельные server/client defines; упрощённый listen-server только для early spike (не production model) |
| **Статус** | `[EXPERIMENT REQUIRED]` |

Listen-server допустим как **временный** инструмент отладки. Production-модель — dedicated (или эквивалент с явной server authority).

**Эксперимент U-DS** — см. §5.9.

---

### 3.5 Serialization

| | |
|--|--|
| **Требование** | Сохранение робота (geometry/components/joints/bindings); передача в матч; версионирование; анти-tamper на уровне валидации схемы |
| **Кандидаты** | ScriptableObject + asset; JSON/MessagePack/бит-пакет; Unity Serialization; custom binary blueprint |
| **Статус** | `[EXPERIMENT REQUIRED]` |

Должен обслуживать: Design → Test → Battle без «ручной» пересборки сцены.

**Эксперимент U-SER**

- **Цель:** round-trip blueprint робота.  
- **Метод:** собрать робота runtime → serialize → new session/scene → deserialize → сравнить иерархию, массы, joints, control map.  
- **Метрики:** bit size; время ser/deser; число расхождений; устойчивость к version bump поля.  
- **Успех:** 100% критичных полей восстанавливаются; робот проходит тот же smoke-тест движения.  
- **Провал:** nondeterministic hierarchy, потеря joints/bindings, неприемлемый размер для net transfer.

---

### 3.6 Scene architecture

| | |
|--|--|
| **Требование** | Быстрый Design ↔ Configure ↔ Test; отдельный Battle flow; минимум loading pain |
| **Кандидаты** | Additive scenes; single persistent app shell + mode scenes; addressables; disable/enable worlds без unload |
| **Статус** | `[EXPERIMENT REQUIRED]` |

**Эксперимент U-SCN**

- **Цель:** измерить переключение Design/Configure/Test.  
- **Метод:** прототип трёх режимов; замер времени до «можно снова управлять»; сохранить робота в памяти между режимами.  
- **Метрики:** median switch time; hitch frames; нужна ли unload физики.  
- **Успех:** переключение ощущается мгновенным для playtest (ориентир: ≪ традиционной level load; целевой порог зафиксировать после первого замера, например &lt; 0.5–1.0 s interactive).  
- **Провал:** обязательные длинные loading gates между правкой и тестом.

---

### 3.7 Runtime physics construction

| | |
|--|--|
| **Требование** | Спавн сложного модульного робота из данных во время runtime (Test/Battle) |
| **Статус** | `[EXPERIMENT REQUIRED]` — критический gate |

См. §5.1.

---

### 3.8 Destruction / detachment

| | |
|--|--|
| **Требование** | Отключение функций; (цель) отрыв компонентов с изменением физики машины |
| **Статус** | `[EXPERIMENT REQUIRED]` |

См. §5.4. MVP может ограничиться disable, но эксперимент на detach нужен рано, чтобы не загнать архитектуру в тупик.

---

### 3.9 Performance characteristics

| | |
|--|--|
| **Требование** | Приемлемый CPU/GPU на клиента; server CPU на N матчей / M роботов; стабильный physics step |
| **Статус** | `[EXPERIMENT REQUIRED]` непрерывный |

Метрики задаются в каждом эксперименте §5; отдельный perf budget document появится после первых цифр.

---

## 4. Кандидаты packages (не утверждены)

Ниже — **исследовательский список**, не backlog установки.

| Область | Примеры для оценки | Статус |
|---------|-------------------|--------|
| Input | Input System (уже в baseline) | Вероятен, подтвердить в UI/control spike |
| Rendering | URP (уже в baseline) | Не блокер physics/net; пересмотр только по perf/art |
| Netcode | NGO / NFE / (иное по результатам) | `[EXPERIMENT REQUIRED]` |
| Transport | Unity Transport и др. | Вместе с netcode |
| Dedicated Server | Dedicated Server package | `[EXPERIMENT REQUIRED]` |
| Multipayer tooling | Multiplayer Play Mode, Network Simulator | Желательны для экспериментов |
| Physics extras | Wheel colliders / custom constraints / Havok | Только после U-PHY |
| Serialization helpers | зависит от U-SER | `[EXPERIMENT REQUIRED]` |

---

## 5. Критические эксперименты (обязательные)

Эти девять проверок закрывают главный технический риск проекта: **модульная физика + multiplayer**.

Общий формат отчёта: `docs/experiments/<id>-<slug>.md` (создавать по мере прохождения).

---

### 5.1 Runtime создание сложного модульного робота

**ID:** EXP-01 · **Теги:** construction, physics, serialization  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Доказать, что робот из N компонентов + joints собирается из данных в runtime |
| **Метод** | Blueprint → instantiate hierarchy → configure rigidbodies/joints/mass → spawn в Test |
| **Метрики** | время сборки; число bodies/joints; стабильность на первом FixedUpdate; ошибки joint setup |
| **Успех** | Робот стабильно спавнится (≥95% попыток без взрыва симуляции); едет/стоит предсказуемо относительно локального дизайна |
| **Провал** | Систематический explosion/jitter; невозможность восстановить связи; время сборки ломает iteration loop |

---

### 5.2 Большое количество rigid bodies и joints

**ID:** EXP-02 · **Теги:** physics, performance  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Найти рабочий диапазон сложности одной машины и двух машин |
| **Метод** | Стресс-сборки: ступенчато увеличивать bodies/joints; профилировать Physics.Processing / main thread |
| **Метрики** | ms/frame physics; fixed timestep overruns; стабильность contacts |
| **Успех** | Определён budget (напр. bodies/joints на робота и на матч 1v1) с запасом; нет постоянных overruns |
| **Провал** | Даже «средняя» машина не укладывается в budget на target hardware |

---

### 5.3 Физическое взаимодействие двух роботов

**ID:** EXP-03 · **Теги:** physics, gameplay feel  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Столкновения двух модульных роботов выглядят честно и читаемо |
| **Метод** | Два runtime-робота; таран, клины, подъём; запись клипов |
| **Метрики** | penetrations; joint breaks (если включены); qualitative fairness score playtest |
| **Успех** | Playtesters могут объяснить исход контакта; нет регулярного tunneling/teleport |
| **Провал** | Хаотичные импульсы, нечитаемый «взрыв», постоянные intersections |

---

### 5.4 Повреждение и отсоединение компонентов

**ID:** EXP-04 · **Теги:** damage, physics  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Проверить disable + detach без разрушения симуляции |
| **Метод** | A) disable motor/weapon mid-sim; B) break joint / detach body; сравнить CoM и управляемость |
| **Метрики** | стабильность после detach; корректность массы/инерции; время на событие |
| **Успех** | Disable надёжен; detach возможен хотя бы для подмножества частей без cascade soft-lock |
| **Провал** | Любой отрыв дестабилизирует всю машину/мир; невозможно продолжить матч |

---

### 5.5 Синхронизация физики по сети

**ID:** EXP-05 · **Теги:** netcode, physics  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Два клиента видят согласованный физический бой (в пределах выбранной authority-модели) |
| **Метод** | Минимальный net spike: 1v1, server sim; сравнить позиции ключевых bodies / исходы столкновений |
| **Метрики** | max positional error proxy; частота hard desync; mismatch исхода (alive/dead, detached parts) |
| **Успех** | Исход боя совпадает у участников; ошибка визуально приемлема на целевом latency |
| **Провал** | Разные победители / разный detach set / постоянный rubber-band |

---

### 5.6 Server-authoritative simulation

**ID:** EXP-06 · **Теги:** netcode, authority  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Клиент не является источником истины для physics state |
| **Метод** | Умышленно «читерский» клиентский impulse; сервер отвергает/перебивает |
| **Метрики** | доля отклонённых незаконных state writes; server reconciliation success |
| **Успех** | Незаконные клиентские силы не меняют authoritative outcome |
| **Провал** | Клиент может телепортировать/ускорять робота без server consent |

---

### 5.7 Client input synchronization

**ID:** EXP-07 · **Теги:** input, netcode  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Actions/bindings игрока надёжно доходят до authoritative симуляции |
| **Метод** | Запись input ticks; воспроизведение на server; сравнить ожидаемые motor commands |
| **Метрики** | lost inputs; reorder rate; задержка input→response |
| **Успех** | При нормальной сети управление предсказуемо; lost input &lt; согласованного порога |
| **Провал** | «Липкие» клавиши, пропуски выстрелов, рассинхрон composite groups |

---

### 5.8 Latency / prediction / interpolation

**ID:** EXP-08 · **Теги:** netcode, feel  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Определить, нужны ли prediction/interpolation и в каком виде |
| **Метод** | Network Simulator: 30/60/100+ ms RTT, jitter, loss; сравнить варианты (raw / interpolate / predict) |
| **Метрики** | perceived responsiveness; visual smoothing; misprediction corrections count |
| **Успех** | На целевом RTT бой playable; выбранная стратегия задокументирована |
| **Провал** | Даже с лучшей стратегией spike unplayable на реалистичном RTT |

---

### 5.9 Dedicated server performance

**ID:** EXP-09 · **Теги:** server, performance  
**Статус:** `[EXPERIMENT REQUIRED]`

| | |
|--|--|
| **Цель** | Headless server тянет authoritative physics матча в budget |
| **Метод** | Dedicated Server build; 1v1 (затем stress N); без rendering cost; профилировать CPU |
| **Метрики** | CPU ms / tick; ticks missed; RAM; сколько параллельных матчей на ref hardware |
| **Успех** | 1v1 стабильно в budget; понятен cost model для scale |
| **Провал** | Один матч уже не влезает в разумный server cost |

---

## 6. Порядок выполнения (рекомендуемый)

```
U-VER (smoke)
  → EXP-01 runtime build
  → EXP-02 stress bodies/joints
  → EXP-03 two-robot interaction
  → EXP-04 damage/detach
  → U-SER + U-SCN (параллельно после EXP-01)
  → выбор physics stack freeze (или явный re-test)
  → EXP-05..08 networking
  → EXP-09 dedicated server
```

Локальная физика и runtime construction **блокируют** серьёзные ставки на netcode.  
Net spike можно начинать на упрощённом роботе, но freeze networking solution — только после EXP-05..08 на репрезентативной сложности.

---

## 7. Decision log

Заполнять по мере решений. Пока пусто — всё важное в `[EXPERIMENT REQUIRED]`.

| Дата | Тема | Решение | Основание (experiment ID) | Статус |
|------|------|---------|---------------------------|--------|
| — | Unity как production engine | **Принято** | Product decision | Fixed |
| | Unity version pin | | U-VER | Open |
| | Physics stack | | EXP-01..04 / U-PHY | Open |
| | Networking solution | | EXP-05..08 | Open |
| | Dedicated server approach | | EXP-09 | Open |
| | Serialization format | | U-SER | Open |
| | Scene architecture | | U-SCN | Open |

---

## 8. Связь с GDD

GDD описывает *что* должно ощущаться. Этот документ описывает *как в Unity это будет доказано*.

Если эксперимент провален — меняют technical approach или (осознанно) design scope MVP, а не «добавляют ещё один package втихую».
