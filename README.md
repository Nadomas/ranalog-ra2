# ra2-analog

Современный духовный наследник **Robot Arena 2**.

Игра про конструирование боевых роботов, их настройку, тестирование и физические бои. Главная ценность — не объём контента, а глубина и свобода инженерного конструирования.

Игрок одновременно **инженер**, **программист управления** и **пилот/тактик**.

## Gameplay loop

1. **Design** — сборка робота (корпус, материалы, компоненты, соединения, масса/мощность/физика)
2. **Configure** — назначение действий компонентов и групп на органы управления
3. **Test Room** — быстрый бесшовный тест конструкции
4. **Battle** — физические бои (в перспективе PvP-first)
5. **Results** — статистика боя

## Принципы

| Принцип | Смысл |
|--------|--------|
| **Multiplayer-first** | Архитектура с учётом сети с самого начала; локальный прототип не усложнять раньше времени |
| **Physics-first** | Физика — ядро геймплея (rigid bodies, joints, damage, detach) |
| **Data-driven** | Компоненты описываются данными; новые детали без переписывания core-систем |
| **Глубина > контент** | Компактный набор деталей, высокая системная глубина |

## Документация

| Документ | Содержание |
|----------|------------|
| [docs/PROJECT_CONTEXT.md](docs/PROJECT_CONTEXT.md) | **Shared project context** (source of truth для агентов) |
| [docs/GDD.md](docs/GDD.md) | **Game Design Document** (полный дизайн) |
| [docs/VISION.md](docs/VISION.md) | Цель, ценность, scope |
| [docs/GAMEPLAY.md](docs/GAMEPLAY.md) | Design / Configure / Test / Battle / Results |
| [docs/TECHNICAL_PRINCIPLES.md](docs/TECHNICAL_PRINCIPLES.md) | Multiplayer, physics, data-driven, engine, economy |
| [docs/UNITY_ENGINE.md](docs/UNITY_ENGINE.md) | **Unity production engine**, tech decisions, experiments |
| [docs/TECHNICAL_ROADMAP.md](docs/TECHNICAL_ROADMAP.md) | **Technical roadmap**, gates, critical path |
| [docs/SDS.md](docs/SDS.md) | **Software/Technical Design Spec** (architecture) |
| [docs/ARCHITECTURE_REVIEW.md](docs/ARCHITECTURE_REVIEW.md) | Architecture review (risks / gates) |
| [docs/TEAM_WORKFLOW.md](docs/TEAM_WORKFLOW.md) | Роли Codex, Cursor и рабочий процесс |

## Репозиторий

- Рабочее название: `ra2-analog`
- Remote: https://github.com/Nadomas/ranalog-ra2.git
- **Production engine: Unity** (внутренние tech-решения — только после экспериментов; см. [UNITY_ENGINE.md](docs/UNITY_ENGINE.md))
- Unity-проект: `UnityProject/ra2-analog/` (baseline: Unity 6000.5.9f1)
- **Unity MCP (Cursor):** `.cursor/mcp.json` → `http://localhost:8080/` (пакет `com.emeryporter.unitymcp`). Нужен запущенный Editor: **Window → Unity MCP → Start**.

## Главный технический риск

Сочетание **сложной физики модульного робота** и **multiplayer**. Roadmap должен максимально рано проверить:

1. стабильную синхронизацию физического боя;
2. масштабируемость архитектуры.
