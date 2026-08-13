# TheDungeon

An **old-school dungeon-crawl exploration RPG** built with **Godot 4.6** and **C#**. Gameplay is **turn-based** (exploration and combat), with procedural dungeons, loot, traps, and character creation.

## Documentation

| Document | Description |
|----------|-------------|
| [Game Design Document](docs/GAME_DESIGN_DOCUMENT.md) | Gameplay and product design — core loop, economy, itemization, UX principles, open questions. |
| [Technical Design Document](docs/TECHNICAL_DESIGN_DOCUMENT.md) | Architecture, runtime/session model, Godot integration rules, testing strategy, ADRs. |
| [Development Baseline (2026-08-07)](docs/PROJECT_STATUS.md) | Dated re-entry map: implemented systems, current dice milestone, editor checklist, and likely next work. |
| [Product Backlog](docs/PRODUCT_BACKLOG.md) | Prioritized feature requests, observed bugs, UX issues, investigations, and unresolved design decisions. |

**AI-assisted contributors:** repository policy for agents (serialized Godot files, tests, architecture) lives in [`.cursor/rules/`](.cursor/rules/) and is summarized in the Technical Design Document.

## Requirements

- **Godot** 4.x (project targets 4.6; see `project.godot` → `config/features`).
- **.NET SDK** 8.0+ (tests target `net8.0`).

## Running the game

Open the project in the **Godot editor** and run the main scene (configured in Project Settings). Do not hand-edit `*.tscn` / `*.tres` in source control for routine changes; use the editor or follow steps in the docs above.

## Tests

From the repository root:

```bash
dotnet test
```

The xUnit project is under `Tests/TheDungeon.Tests/`. See [Technical Design — Testing strategy](docs/TECHNICAL_DESIGN_DOCUMENT.md#testing-strategy) for scope and expectations.
