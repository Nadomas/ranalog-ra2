# Gameplay — ra2-analog

Базовый цикл: **Design → Configure → Test Room → Battle → Results**.

Референс механик: [`ORIGINAL_AS_IS.md`](ORIGINAL_AS_IS.md). Ниже — **целевые требования** по духу Robot Arena 2, не побайтовый клон.

Критично: переходы **Design ↔ Configure ↔ Test** — максимально быстрые и желательно бесшовные.

---

## 1. Design (Workshop)

Игрок создаёт робота как **физическую систему** в порядке, близком к оригиналу:

```
Chassis (форма + броня) → Components (детали) → [далее Configure: Wiring]
```

### 1.1 Chassis (корпус)

| Требование | Референс RA2 |
|------------|--------------|
| Контур baseplate на grid, замкнутый полигон | ≤ **16** точек, без самопересечений |
| Step 2: верхняя панель + высота (extrude) | клинья, скосы |
| Выбор **брони** после формы | Polymer, Aluminum, Titanium, Steel — strength ↔ weight |
| **Weight class** робота | Lightweight / Middleweight / Heavyweight (лимиты массы — `[EXPERIMENT REQUIRED]`) |
| После установки деталей форма chassis **заморожена** | смена формы сбрасывает размещённые части |
| Размещение на baseplate **внутри** корпуса | invalid = визуально «красный» |

### 1.2 Components (детали)

Категории (UI/данные), как у референса:

| Категория | Примеры |
|-----------|---------|
| **Power** | Control Board (обязателен), Battery, Air Tank |
| **Mechanics** | Spin / Burst / Servo motor; Burst / Servo piston; Linear Actuator |
| **Treads** | Wheels (только на **axle** мотора) |
| **Weapons** | Ударные головы, лезвия, ram plates… |
| **Extenders** | Стержни, угловые соединители, крепления |
| **Extras** | Ballast, wedges, SmartZone, … |

Правила размещения (обязательны по смыслу):

- тело мотора/поршня **не** сквозь стенку chassis; **ось / шток** — можно;
- колёса **только** на осях, не на baseplate;
- Shift — поворот детали; Ctrl — подъём над baseplate;
- зелёные **attachment points** задают точку крепления.

### 1.3 Энергия (dual resource)

| Ресурс | Питает | Деф-поля (data) |
|--------|--------|-----------------|
| **Electricity** | Motors (spin/burst/servo) | `electotal`, `elecMaxInOutRate` |
| **Compressed air** | Pistons (burst/servo) | `airtotal`, `airmaxinoutrate` |
| **Оба** | Гибридные боты (ход + удар поршнем) | battery + air tank |

Нехватка бюджета → деградация/отказ приводов в Test и Battle.

---

## 2. Configure (Wiring / Controller)

**DESIGN ≠ CONFIGURE:** управление проектируется **отдельно** от геометрии.

Без **Control Board** wiring недоступен.

### 2.1 Controller (пульт)

Три типа **controls** на сетке слотов:

| Control | Семантика |
|---------|-----------|
| **Switch** | toggle on/off |
| **Button** | power пока зажато; one-shot **Fire** для burst |
| **Analog** | −100…+100; клавиатура ≈ ±100%; стик — частичная мощность |

Каждому control: имя + привязка к клавише/кнопке геймпада.

### 2.2 Wiring

Поток: выбрать control → кликнуть компонент → выбрать **канал**:

| Actuator | Каналы wiring |
|----------|----------------|
| Spin motor | CW / CCW |
| Burst motor / burst piston | **Fire** |
| Servo motor | bi-dir (analog) |
| Servo piston / linear actuator | extend / retract |
| Tank drive (типично) | **Forward-Back** + **Left-Right** analog → колёса |

**Tank / differential steer** — эталонная схема (2 analog), не единственная: игрок может собрать иное.

### 2.3 Groups / composite actions (наша абстракция)

`groups` + `composite actions` в коде/данных — **эквивалент** нескольким wiring-линиям с одного control (например WASD → 4 колеса).  
Жёстко зашивать только WASD в код **нельзя**; tank-steer — рекомендуемый онбординг-паттерн.

---

## 3. Test Room (Practice Garage)

Мгновенный тест текущей конструкции — **тот же физический мир**, что Battle.

Проверяет:

- ошибки конструкции и attachment;
- wiring / named controls;
- нехватку electric или air;
- баланс массы / CoM;
- поведение spin/burst/servo и pistons.

Опциональные препятствия (референс): barrels, blocks, cones, ramps — для стресс-теста без матча.

---

## 4. Battle

Бои **физические**, server-authoritative в сети.

### 4.1 Режимы (целевой набор, как RA2)

| Режим | Суть |
|-------|------|
| **Deathmatch** | последний / больше урона |
| **Battle Royal** | FFA elimination |
| **Team Match** | команды |
| **Tabletop** | арена с краем; выбивание за борт → eliminate |
| **King of the Hill** | удержание зоны → очки |

MVP: один режим с читаемым исходом (ориентир — **1v1 Deathmatch** + immobility); остальные — post-MVP по приоритету.

### 4.2 Победа / поражение

**Основной путь (как RA2):** **immobility** — робот не может двигаться N секунд → поражение (countdown UI).  
Дополнительно: eliminate (pit / tabletop), KOTH по очкам, таймер матча как tie-break.

Точные пороги immobility и tie-break — `[EXPERIMENT REQUIRED]`.

### 4.3 Damage в бою

| Слой | Требование |
|------|------------|
| Hit по **chassis** | splash к внутренним деталям по **дистанции** от точки удара |
| Оружие | mix **concussion** + **piercing** (data-driven 0–1) |
| Деградация | снижение эффективности; **не** мгновенный hard-off всего |
| Fracture / detach | отрыв модулей меняет топологию; net-detach — после EXP |
| SmartZone | опциональный сенсор контакта (оружие/триггеры) |

---

## 5. Results

После боя — разбор для следующей итерации Design:

- исход и **причина** (immobilized, eliminated, KOTH, timer…);
- нанесённый / полученный урон;
- повреждённые / отключённые / оторванные компоненты;
- время активности / мобильность;
- (post-MVP) эффективность оружия, critical hits.

Results подталкивают в Workshop, а не только показывают счёт.
