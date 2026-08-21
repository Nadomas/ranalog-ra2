# Technical principles — ra2-analog

## Multiplayer-first

Мультиплеер — не «фича после синглплеера». Архитектура учитывает сеть с начала.

Нужно заранее закладывать:

- network synchronization;
- authoritative simulation;
- состояние физических объектов, компонентов, управления;
- connect / disconnect;
- lobby;
- matchmaking;
- latency;
- prediction / interpolation (если нужно);
- anti-cheat;
- reproducibility физических ситуаций;
- передача состояния робота и событий урона;
- будущий replay / spectate.

### Ранний баланс сложности

Multiplayer **не должен** преждевременно усложнять локальный прототип.

На ранних этапах — **минимально жизнеспособная** multiplayer-архитектура, чтобы рано ответить:

1. можно ли стабильно синхронизировать физический бой;
2. насколько архитектура масштабируется.

---

## Physics-first

Физика — одна из основных механик.

Учитывать:

- rigid bodies, collision, joints;
- suspension, wheels, motors, torque;
- mass, center of mass, friction, inertia;
- detachable / destroyable components;
- damage;
- physical interaction между роботами;
- потенциальное разрушение отдельных элементов.

---

## Data-driven

Компоненты описываются **данными**, насколько это разумно.

Добавление новой детали не должно требовать переписывания core-систем.

Пример: новый ротор = характеристики + доступные actions, а не отдельная уникальная ветка всей логики управления игрой.

---

## Economy (опционально)

Экономика **не** обязательна для core gameplay.

Возможна карьера:

- деньги / ресурсы;
- покупка деталей;
- улучшение робота;
- турниры;
- прогрессия.

Риск: экономика ломает PvP-баланс.

Поэтому экономика — **отдельный слой** поверх core.

В PvP баланс должен опираться на:

- конструкцию;
- выбор компонентов;
- инженерные решения;
- навык игрока.

Не делать обязательным pay-to-win или progression-based power advantage.

---

## Engine

**Production engine зафиксирован: Unity.**

Unreal Engine и другие движки **не** рассматриваются как production-альтернативы.

Конкретные решения внутри Unity (версия, physics stack, networking, dedicated server, serialization, scene architecture, runtime construction, destruction, performance) **не** принимаются «по умолчанию». Они исследуются и валидируются экспериментально.

Если решение нельзя уверенно принять теоретически — статус **`[EXPERIMENT REQUIRED]`** с методом, метриками и критериями успеха.

Канонический документ: [UNITY_ENGINE.md](UNITY_ENGINE.md).

---

## Главный технический риск

**Сложная физика модульного робота + multiplayer.**

Roadmap обязан максимально рано проверить именно этот риск (см. критические эксперименты EXP-01…EXP-09 в [UNITY_ENGINE.md](UNITY_ENGINE.md)).
