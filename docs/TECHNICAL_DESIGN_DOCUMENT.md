# TheDungeon — Technical Design Document

This document is the **canonical reference for engineering and architecture** in this repository. Workspace rules under [`.cursor/rules/`](../.cursor/rules/) remain the **enforcement layer for AI-assisted editing** (especially Godot serialized assets and testing expectations); this file provides the full narrative and links humans and tools to the same decisions.

---

## Runtime, session state, and composition root

### `GameSessionState`

[`GameSessionState`](../Scripts/2.State/GameSessionState.cs) is the **per-run aggregate** for gameplay data:

- **`Player`** — [`PlayerState`](../Scripts/2.State/Player/PlayerState.cs) (stats, inventory, purse, abilities, etc.).
- **`Dungeon`** — [`DungeonState`](../Scripts/2.State/Dungeon/DungeonState.cs) (floors, player coordinate, exploration vs combat mode, discovery).
- **`Combat`** — optional [`CombatState`](../Scripts/2.State/Combat/CombatState.cs) while a fight is active.
- **`Phase`** — [`GamePlayPhase`](../Scripts/2.State/Player/GamePlayPhase.cs) (e.g. run in progress vs game over).
- **Logging** — narrative/game log via `AppendGameLog` / `AppendLog`, with optional log archival.

`ResetForNewRunPreservingFallenRecord()` clears dungeon and combat, resets the player for a new run, and preserves fallen-adventurer metadata for related features.

### `GameRunContext`

[`GameRunContext`](../Scenes/GameRunContext.cs) is assembled once per new game from [`GameRoot.BuildGameRunContext()`](../Scenes/GameRoot.cs). It **does not** replace `GameSessionState`; it **bundles** the session with **services and repositories** used by UI and game logic (exploration, combat, dice, narrative, repositories, trap/treasure/container flows, character creation, etc.). Scene code receives this context after character creation when main UI starts.

**Composition root:** [`GameRoot`](../Scenes/GameRoot.cs) constructs concrete [`Godot*Repository`](../Repositories/) implementations from exported Godot databases and passes **interfaces** defined in Core into services. That matches the tier rule: low tiers depend on abstractions; Godot wiring sits at the root.

### Phases and dungeon mode

- **Session phase** — `GameSessionState.Phase` drives top-level run flow (e.g. game over vs continuing).
- **Dungeon mode** — `DungeonState.DungeonMode` distinguishes **exploration** from **combat** ([`DungeonMode`](../Scripts/2.State/Dungeon/DungeonMode.cs)), toggled when combat starts or ends.

### Save / load model

**Not implemented for gameplay persistence today.** [`GameRoot.HandleLoadGame()`](../Scenes/GameRoot.cs) is empty; there is no serialization pipeline documented here beyond log archiving hooks on `GameSessionState`. Any future save/load belongs in this section once designed.

---

## Dungeon generation (implementation)

### Procedural generation

The intended **player-facing** dungeon experience is **procedural** (see [Game Design Document — Dungeon structure](./GAME_DESIGN_DOCUMENT.md#dungeon-structure)). [`DungeonBootstrap.CreateInitialFloor(true)`](../Scripts/3.Game/Dungeon/DungeonBootstrap.cs) uses [`FloorGenerator`](../Scripts/3.Game/Dungeon/FloorGenerator.cs) with [`FloorGenerationParameters`](../Scripts/3.Game.Contracts/Dungeon/FloorGenerationParameters.cs) supplied from [`GameBalanceSettingsResource`](../Resources/GameBalanceSettingsResource.cs) via [`GameBalanceSettingsMapper`](../Mappers/GameBalanceSettingsMapper.cs).

### Hand-built floor (`DungeonBootstrap` / `HandBuiltDungeonFloor`)

[`DungeonBootstrap.CreateInitialFloor(false)`](../Scripts/3.Game/Dungeon/DungeonBootstrap.cs) returns [`HandBuiltDungeonFloor.CreateSample(...)`](../Scripts/3.Game/Dungeon/HandBuiltDungeonFloor.cs). This path exists for **tests and development** (e.g. [`HandBuiltDungeonFloorTests`](../Tests/TheDungeon.Tests/HandBuiltDungeonFloorTests.cs)) and for toggling a fixed layout when debugging. [`ExplorationUiPresenter`](../Scripts/4.UI/Presentation/ExplorationUiPresenter.cs) chooses procedural vs hand-built via a **private bool** (`_useProceduralFloor`, currently `true` for procedural).

**This is not a product design pillar** — do not document it in the GDD as a player-facing mode. See [Game Design Document](./GAME_DESIGN_DOCUMENT.md).

---

## Assembly tiers and references

Projects under `Scripts/` are **layered**. **A project must not reference a project in a higher tier.**

| Tier | Project | May reference |
|------|---------|---------------|
| Foundation | `0.Core` (`TheDungeon.Core`) | *(none of the other script projects)* |
| Parallel on Core | `2.State`, `3.Game.Contracts` | `0.Core` only |
| Game rules | `3.Game` (`TheDungeon.Game`) | `0.Core`, `2.State`, `3.Game.Contracts` |
| Presentation | `4.UI` | `0.Core`, `2.State`, `3.Game`, `3.Game.Contracts` |

**`3.Game.Contracts`** holds **DTOs, result types, and shared request/response shapes** used across Game and UI. It must stay **thin**: no references upward, and no game-rule implementation—only types that describe data crossing boundaries.

**Composition root:** Code that talks to Godot resources and the scene tree (e.g. [`GameRoot`](../Scenes/GameRoot.cs), [`Repositories/Godot*Repository.cs`](../Repositories/)) lives **outside** this strict stack but **wires** implementations into interfaces declared in lower tiers. Services depend on **abstractions** (e.g. `I*Repository` in Core), not on Godot-specific types.

---

## Domain models and DTOs

- **Domain types** (in Core) and **DTOs** (especially in Contracts) are **dumb containers**: properties, nested types, maybe simple enums.
- **Do not** embed game rules, validation orchestration, or multi-step workflows inside these types.
- **Behavior** belongs in **services**, **state mutation** code, and **helpers**. Prefer making invalid states unrepresentable via **construction and APIs** at those layers rather than “smart” domain objects.

---

## Dependency injection vs statics

- **Prefer explicit injection:** pass dependencies via constructors or small context objects (e.g. `GameRunContext`) assembled once per run.
- Avoid **static mutable** service state and **service locator** patterns for core game logic.
- **`static`** is appropriate for **pure functions**, constants, and small **stateless helpers**—not as a default way to reach services.

---

## UI reactivity and events

As features multiply, **many services** can affect what the UI should show. Prefer:

1. **Events / notifications** raised when meaningful game state changes (combat resolved, inventory changed, floor revealed, etc.).
2. **UI/presenter layers** that **subscribe** and map those signals to controls.

Reserve **direct** calls like `RefreshX()` from deep services into specific panels for **simple** flows or transitional code; new work should **bias toward** listener-style updates.

---

## C# object construction

- **Default style:** types that need several field/property assignments should use a **parameterless constructor** plus **object initializer**:

  ```csharp
  var x = new MyRecord
  {
      A = a,
      B = b,
  };
  ```

- **When to use non-default constructors:** unavoidable invariants, **immutable** aggregates, **Godot** lifecycle and `[Export]` usage, performance-critical allocations, or consistency with an existing family of types that already uses rich constructors.

---

## Repositories and content pipeline

- **Definitions** — Core interfaces (`I*DefinitionRepository`) describe lookup by id; Godot [`Resources/`](../Resources/) subclasses hold serialized data; [`Mappers/`](../Mappers/) map resources to Core definitions.
- **Bootstrap** — [`GameRoot`](../Scenes/GameRoot.cs) exports `*ResourceDatabase` references and constructs `Godot*DefinitionRepository` implementations.

Policies:

- **Duplicate IDs** — warn + last-wins (ADR-0003).
- **`TryGetById`** — returns `null` for unknown IDs without per-lookup warnings (ADR-0004).
- **Missing databases** — no silent implicit fallback defaults; missing content should surface explicitly (ADR-0005).
- **Items** — [`GodotItemDefinitionRepository`](../Repositories/GodotItemDefinitionRepository.cs) merges embedded monster death-loot item resources with `ItemDatabase` per ADR-0009. **Authoring table and precedence** — [Monster death loot authoring (embedded vs standalone)](#monster-death-loot-authoring-embedded-vs-standalone). **Product intent (junk vs systemic items)** — [GDD — Itemization philosophy](./GAME_DESIGN_DOCUMENT.md#itemization-philosophy).

---

## Monster death loot authoring (embedded vs standalone)

This section is the **content-pipeline contract** for corpse drops. The **player-facing split** (junk vs items that participate in other systems) is stated in the [GDD — Itemization philosophy](./GAME_DESIGN_DOCUMENT.md#itemization-philosophy). **ADR-0009** in the appendix records the formal decision.

### Where to author items

| Situation | Where to author the item |
|-----------|--------------------------|
| **Trash / vendor fodder only** — sell value, no crafting, alchemy, quests, or shared drops | May stay **embedded** as `ItemResource` sub-resources on the monster’s `MonsterResource` (`DeathLoot` rows). Keeps file count down and drops local to the creature. |
| **Any other use** — recipes, alchemy, traps, chest tables, NPC grants, shared loot pools, or anything that should appear in a central catalog | Use a **standalone** `.tres` item under `Content/Item/…` and register it in **`ItemDatabase`**. Reference that asset from death loot. |

When an embedded trash item **later** gains a non-trash use, **move** it to a standalone item resource and the database so other systems have a single source of truth.

### Runtime precedence

Definitions from **`ItemDatabase`** take precedence over the same id merged from monster death loot ([`GodotItemDefinitionRepository`](../Repositories/GodotItemDefinitionRepository.cs); see **ADR-0009** implementation note below).

---

## Godot integration boundaries (serialized assets)

These rules align with [ADR-0001](#adr-0001-godot-serialized-files-are-editor-owned) and [`.cursor/rules/godot-no-tscn-edits.mdc`](../.cursor/rules/godot-no-tscn-edits.mdc).

### Non-negotiable

- **Never** edit `*.tscn`, `*.tres`, or other Godot **serialized** scene/resource text in agent-driven code changes (including “small fixes,” `script = null` removals, export path lines, or `ext_resource` tweaks).
- **Never** add new `.tres` / serialized resources on disk via the agent.

### What to do instead

- When scene trees, node properties, exports, or resources need to change: provide **exact, step-by-step Godot Editor instructions** (which scene to open, which node, Inspector fields, `NodePath` / export targets, sub-resources, etc.) so a human applies the change in the editor.
- **Assume** editor wiring exists. **Write C# accordingly**: normal `[Export]` fields, straightforward references, and clear `GD.PushWarning` / errors when something is missing—not elaborate runtime discovery (`GetNode`/`FindChild` trees, duplicate code paths, “try export else scan the tree,” etc.) to replace editor-owned data.

### Scripts

- Editing **C#** (`.cs`) attached to scenes or `[GlobalClass]` resources is fine.
- Prefer designs where **the editor owns** serialized layout and exports; **code owns** behavior.

---

## UI architecture (coordinator / presenter)

Keep scene scripts thin; move orchestration and heavy interaction logic into coordinator/presenter classes. Preferred shape: `MainUi` → `GameUiCoordinator` → presenters. See ADR-0002 in the appendix.

---

## 3D dice presentation

Authoritative rolls stay in [`DiceRollService`](../Scripts/3.Game/Services/DiceRollService.cs). The overlay only **displays** already-resolved faces. Presentation is **fire-and-forget** today (narrative/combat do not await dice); gating outcomes on animation completes is deferred to the game-wide async feature.

### Flow

1. Game code rolls (`DiceRollService` / `ResolutionService.RollAgainstTarget`).
2. [`PhysicalDieRollExtractor`](../Scripts/3.Game/Helpers/PhysicalDieRollExtractor.cs) maps `DiceRollResult` → [`PhysicalDieRollSpec`](../Scripts/3.Game.Contracts/Dice/PhysicalDieRollSpec.cs) (d100 → percentile tens + ones d10).
3. [`IDiceRollPresenter`](../Scripts/3.Game.Contracts/Dice/IDiceRollPresenter.cs) (`GodotDiceRollPresenter` → [`DiceRollOverlay`](../Scenes/Components/Dice/DiceRollOverlay.cs)) spawns [`RollingDie`](../Scenes/Components/Dice/RollingDie.cs) wrappers concurrently.
4. Each roll: physics toss → optional face snap → `PostSnapDisplaySeconds` freeze → overlay `DieLingerSeconds` → `QueueFree`.

### Scene model

- **`RollingDie`**: `Node3D` wrapper (spawn offset, orchestration).
- **`*_visual.tscn`**: root **`RigidBody3D`** with mesh, **convex `CollisionShape3D`**, and [`DieFaceCalibration`](../Scenes/Components/Dice/DieFaceCalibration.cs) on the same node (or child).
- **No** nested `RigidBody3D` under another `RigidBody3D`. Containment uses **floor + [`DicePlayAreaWalls`](../Scenes/Components/Dice/DicePlayAreaWalls.cs)** (no post-roll teleport clamp).

### Visual catalogs

- [`DieVisualCatalog`](../Resources/DieVisualCatalog.cs): one **situation** (`DieRollVisualKind` — Player, Monster) + entries (`DieType`, `DieVisualRole`, `PackedScene`).
- [`DieVisualCatalogLibrary`](../Resources/DieVisualCatalogLibrary.cs): array of catalogs; overlay resolves `(situation, dieType, role)`.
- d100 percentile tens may be keyed as `d10` + `PercentileTens` or `d100` + `PercentileTens` ([`DieVisualCatalogKeys`](../Scripts/3.Game/Helpers/DieVisualCatalogKeys.cs)).

### Face calibration (Quaternion, not Euler)

Per face on `DieFaceCalibration`: **`FaceOrientation` (`Quaternion` x,y,z,w)** — die rotation where that face is the intended “up” read face before camera snap. **Do not** enter Euler degrees (X,Y,Z); those are not face directions.

Snap: calibration quaternion → align face toward camera → random spin around camera axis ([`DieFaceOrientationSolver`](../Scripts/3.Game/Helpers/DieFaceOrientationSolver.cs)).

### d100 display rules

[`PercentileDiceFaceMapper`](../Scripts/3.Game/Helpers/PercentileDiceFaceMapper.cs): e.g. 24 → tens 20 + ones 4; 6 → 00 + 6; 30 → 30 + 10; 100 → 00 + 10.

### Godot editor checklist (human-owned)

1. **`RollingDie.tscn`**: root `Node3D`; remove generic sphere collider and baked visual children.
2. **Each `*_visual.tscn`**: root `RigidBody3D`; convex collider; `DieFaceCalibration` with `FaceOrientation` per face.
3. **`dice_roll_overlay`**: assign `RollingDieScene`, `DiceSpawnPath`, `CameraPath` (→ `DiceWorld/Camera3D`), `VisualCatalogLibrary`; add `DicePlayAreaWalls` under `DiceWorld`; tune wall half-extents to spawn bounds.
4. **`main_ui.tscn`**: export `DiceRollOverlay` on `MainUi`.
5. **Catalog `.tres`**: `PlayerDieVisualCatalog`, `MonsterDieVisualCatalog`, wrapped in `DieVisualCatalogLibrary`.
6. **`dice_test_scene`**: wire script exports; remove baked `RollingDie` under spawn root; floor + walls + camera.

### Test harness

[`dice_test_scene`](../Scenes/Components/Dice/dice_test_scene.tscn) + [`DiceTestScene.cs`](../Scenes/Components/Dice/DiceTestScene.cs): per-die spawn, free roll, gameplay roll (forced face), d100 pair, calibration verify, **Clear all**. `PersistDiceUntilClear` (default on) vs overlay-style auto-remove with linger.

---

## Testing strategy

This section incorporates the former [`docs/TESTING_POLICY.md`](./TESTING_POLICY.md) (stub) and **ADR-0008**.

### Current unit-test scope

The default automated harness is **xUnit** under [`Tests/TheDungeon.Tests/`](../Tests/TheDungeon.Tests/).

**In scope** for routine feature work:

- Pure C# logic in `Scripts/0.Core/`
- Pure C# logic in `Scripts/2.State/`
- Pure C# logic in `Scripts/3.Game/`

These areas should ship with both **happy-path** and **fail-path** tests when changed.

### Temporary out-of-scope areas

The following are intentionally deferred in the current xUnit-only workflow:

- `Mappers/`
- `Repositories/` behavior that depends on Godot runtime/resource semantics
- `Resources/` data/resource wiring behavior

This boundary avoids brittle failures in non-Godot-hosted test execution. When code changes touch those areas, note in PR/review that coverage is deferred per this policy.

### Revisit trigger

Revisit when a stable **Godot-hosted integration test harness** exists; add mapper/repository/resource coverage there rather than forcing it into the xUnit runtime.

### Former backlog (now covered in xUnit)

Non-exhaustive examples exercised under `Tests/TheDungeon.Tests/`:

| Code | Tests |
|------|-------|
| [`PlayerVitalsService`](../Scripts/3.Game/Services/PlayerVitalsService.cs) | `PlayerVitalsServiceTests.cs` |
| [`CombatAbilityCooldowns`](../Scripts/2.State/Combat/CombatAbilityCooldowns.cs) | `CombatAbilityCooldownsTests.cs` |
| [`CharacterNameValidator`](../Scripts/3.Game/CharacterNameValidator.cs) | `CharacterNameValidatorTests.cs` |
| [`CharacterCreationService`](../Scripts/3.Game/Services/CharacterCreationService.cs) | `CharacterCreationServiceRollTests.cs`, `CharacterCreationServiceApplyTests.cs` |
| [`GameSessionState` logging](../Scripts/2.State/GameSessionState.cs) | `GameSessionStateLogTests.cs`, `GameSessionStateResetTests.cs` |
| [`DefendCombatAbilityHandler`](../Scripts/3.Game/Combat/DefendCombatAbilityHandler.cs) | `DefendCombatAbilityHandlerTests.cs` |

Additional combat/UI/dungeon coverage includes `CombatTurnLoopTests.cs`, `CombatMonsterTurnTests.cs`, `CombatUiPresenterTests.cs`, `HandBuiltDungeonFloorTests.cs`, and `NarrativeServiceContractTests.cs`.

### Feature-test checklist (default)

For each new feature or logic change:

- Add at least one happy-path test.
- Add at least one fail/edge-path test.
- Prefer deterministic seams (fixed random/stubs) over probabilistic assertions.
- If change is in temporary out-of-scope folders, explicitly record deferred coverage in PR notes.

**Workspace rule:** [`.cursor/rules/testing-requirements.mdc`](../.cursor/rules/testing-requirements.mdc) requires shipping tests with features and running `dotnet test` before considering work complete.

---

## Extension patterns (summary)

- **New definition types** — Core definition + id constants + resource + mapper + `Godot*Repository` registration from [`GameRoot`](../Scenes/GameRoot.cs) when needed.
- **New rules** — Prefer `Scripts/3.Game` services and pure helpers under tier boundaries; expose narrow contracts to UI via Contracts DTOs where appropriate.
- **New UI** — Prefer presenters/coordinators over growing scene scripts (ADR-0002).

---

## Godot editor procedures (operational)

Serialized wiring is human-applied in the Godot editor. Example step lists:

- [Abilities: remaining Godot editor steps](../Documentation/Abilities-Godot-setup.md)

Add similar focused docs under `Documentation/` for other features as needed.

---

## Appendix: Architecture Decision Records

Concrete decisions previously recorded in [`docs/ARCHITECTURE_DECISIONS.md`](./ARCHITECTURE_DECISIONS.md) (stub). Status and wording preserved.

### ADR-0001: Godot serialized files are editor-owned

- **Status:** Accepted
- **Decision:** Do not edit `*.tscn`, `*.tres`, or other Godot serialized resources in code changes.
- **Rationale:** Text edits to serialized Godot files are brittle and can break scene/resource wiring.
- **Implementation rule:** C# scripts may be changed; scene/resource wiring changes are provided as explicit Godot Editor steps and applied in the editor.

### ADR-0002: UI architecture standard is coordinator/presenter

- **Status:** Accepted
- **Decision:** Keep scene scripts thin; move orchestration and heavy interaction logic into coordinator/presenter classes.
- **Rationale:** Improves testability, reduces scene script sprawl, and avoids competing UI patterns.
- **Scope notes:**
  - `MainUi -> GameUiCoordinator -> presenters` is the preferred model.
  - `NotebookOverlay` heavy logic should be progressively extracted into a coordinator/presenter class rather than expanded inline.

### ADR-0003: Repository duplicate-id policy

- **Status:** Accepted
- **Decision:** For definition repositories, duplicate IDs are handled as **warn + last-wins**.
- **Rationale:** Keeps startup resilient while still surfacing content issues.
- **Implementation rule:** Map input definitions, group by ID, warn for groups with duplicates, keep the last entry per ID.

### ADR-0004: Repository lookup policy (`TryGetById`)

- **Status:** Accepted
- **Decision:** `TryGetById` returns `null` quietly for unknown IDs (no per-lookup warning by default).
- **Rationale:** Avoids noisy logs in normal control flow where misses may be expected.
- **Future note:** When debug diagnostics logging is expanded, optional centralized debug-level lookup tracing may be added.

### ADR-0005: Missing-database fallback policy

- **Status:** Accepted
- **Decision:** Remove implicit fallback default data for missing databases now that prototyping has passed.
- **Rationale:** Missing content should be visible and explicit, not silently replaced.
- **Exception:** Explicit/manual gameplay bootstrap data can exist where intentionally designed and documented.

### ADR-0006: State object helper boundary

- **Status:** Accepted
- **Decision:** State objects may include lightweight helpers and direct data manipulation.
- **Rationale:** Small, local helpers improve ergonomics without forcing unnecessary service indirection.
- **Boundary:** Cross-system orchestration and heavier rule logic remain in service/helper layers.

### ADR-0007: Armor damage-reduction targeting and lookup

- **Status:** Accepted
- **Decision:**
  - `DamageReductionEffectDefinition` targeting precedence is:
    1. `DamageType` set -> applies only to that damage type.
    2. `DamageType` unset + `DamageFamily` set -> applies to that family.
    3. both unset -> applies to all damage.
  - Aggregation stays on `PlayerState` as data fields; lookup logic is handled by `PlayerDamageReductionHelper`.
- **Rationale:** Keeps serialized effect data compact and unambiguous, avoids duplicate armor DR storage, and keeps query logic out of state shape while remaining reusable beyond combat.

Player-facing summary: [Game Design Document — Combat and damage](./GAME_DESIGN_DOCUMENT.md#combat-and-damage).

### ADR-0008: Temporary automated test-scope boundary

- **Status:** Accepted
- **Decision:** The default xUnit harness focuses on pure C# logic under `Scripts/0.Core`, `Scripts/2.State`, and `Scripts/3.Game`. Automated tests for `Mappers/`, `Repositories/` Godot runtime/resource behavior, and `Resources/` wiring are deferred for now.
- **Rationale:** These areas are better validated in a stable Godot-hosted integration harness; forcing them into the non-Godot xUnit runtime causes brittle behavior and poor signal.
- **Scope notes:**
  - New or changed code in in-scope areas should include happy-path and fail-path tests.
  - Changes in deferred areas should explicitly note deferred coverage in PR/review notes.
- **Revisit condition:** Revisit this boundary when a reliable Godot-hosted integration test workflow is available, then add mapper/repository/resource coverage there.
- **Reference:** [Testing strategy](#testing-strategy) in this document (formerly `docs/TESTING_POLICY.md`).

### ADR-0009: Monster death loot — embedded vs standalone items

- **Status:** Accepted
- **Decision:**
  - **Embedded** `ItemResource` sub-resources on `MonsterResource.DeathLoot` are allowed **only** for **trash / purely sellable** drops (no crafting, alchemy, quests, or other cross-system use).
  - Items that **have or may gain** other uses (e.g. alchemy, recipes, shared loot tables, NPC scripting) must live as **standalone** item `.tres` files under `Content/Item/…` and be registered in **`ItemDatabase`** (and referenced from death loot), so there is one authoritative definition.
- **Rationale:** Fewer files for simple junk loot; central catalog and reuse when an item matters beyond “sell from corpse.”
- **Implementation note:** `GodotItemDefinitionRepository` merges embedded death-loot item resources into lookup when absent from `ItemDatabase`; `ItemDatabase` wins on duplicate ids.
- **Reference:** Product intent — [Game Design Document — Itemization philosophy](./GAME_DESIGN_DOCUMENT.md#itemization-philosophy). Authoring contract — [Monster death loot authoring (embedded vs standalone)](#monster-death-loot-authoring-embedded-vs-standalone).
