# Loot, containers, and phased delivery

Single checklist for container-backed loot. Reference in chats: `@docs/LOOT_CONTAINERS_ROADMAP.md`.

## Scope boundaries

- **In-editor:** New `[Export]` arrays on resources are wired in the Godot Inspector (agent does not edit `*.tscn` / serialized `*.tres` per repo rules).
- **Runtime:** Domain types in `Scripts/0.Core`; behavior in `Scripts/3.Game`; Godot `Resource` subclasses under `Resources/`; mappers under `Mappers/`.

## Phase 1 — Foundation (current)

**Goal:** `LootableItemDefinition`, `LootableItemResource`, mappers, `ContainerFeature` hierarchy (`SalvageFeature`, `CorpseFeature`, `ChestFeature`), `InventoryState.AddOrStack` for quantities, `ContainerLootOperations` + narrative lines, capacity peek helper, unit tests.

Successful trap disarm stages a **`SalvageFeature`** with loot rows ([`TrapService.ApplySuccess`](../Scripts/3.Game/Services/TrapService.cs)); transfer to inventory uses **`ContainerLootInteractionService`** / **`ContainerLootOperations`** — nothing is added directly to the backpack at disarm time.

**Exit criteria**

- [x] Types and services compile; `dotnet test` green.
- [x] Transfer from container to inventory is **explicit** (no silent backpack fills from disarm—that remains Phase 2).

**Key files**

- [`LootableItemDefinition`](../Scripts/0.Core/Inventory/LootableItemDefinition.cs)
- [`ContainerFeature` hierarchy](../Scripts/0.Core/Dungeon/)
- [`LootableItemResource`](../Resources/LootableItemResource.cs)
- [`LootableItemMapper`](../Mappers/LootableItemMapper.cs)
- [`TrapDefinition` / `TrapResource` disarm loot](../Scripts/0.Core/Trap/TrapDefinition.cs), [`TrapMapper`](../Mappers/TrapMapper.cs)
- [`ContainerLootOperations`](../Scripts/3.Game/Services/ContainerLootOperations.cs)

## Phase 2 — Traps

**Goal:** [`TrapService.ApplySuccess`](../Scripts/3.Game/Services/TrapService.cs) stages a **`SalvageFeature`** from **`TrapDefinition.DisarmLoot`**; remove [`TrapIds.Snare`](../Scripts/0.Core/Trap/TrapIds.cs) / [`InventoryIds.Rope`](../Scripts/0.Core/Inventory/InventoryIds.cs) branching.

**Exit criteria**

- [x] Disarm tests updated ([`TrapServiceDisarmTests`](../Tests/TheDungeon.Tests/TrapServiceDisarmTests.cs)).

## Phase 3 — Picker contract

**Goal:** UI-agnostic API + DTOs (`open container`, `loot selected`, `loot all`, `close`); optional stub implementation.

**Exit criteria**

- [x] Presenter/tests can drive loot without Godot scenes.

**Key files**

- [`ContainerLoot*Dto` / `ContainerLootErrorCode`](../Scripts/3.Game.Contracts/Inventory/) (Contracts)
- [`ContainerLootInteractionService`](../Scripts/3.Game/Services/ContainerLootInteractionService.cs), [`RoomContainerLocator`](../Scripts/3.Game/Services/RoomContainerLocator.cs), [`ContainerLootOperations`](../Scripts/3.Game/Services/ContainerLootOperations.cs)

## Phase 4 — Monsters

**Goal:** On death → **`CorpseFeature`** with loot from monster definition; room presence / flee rules when ready.

**Exit criteria**

- [x] Death loot on player kill ([`CombatCorpseHelper`](../Scripts/3.Game/Services/CombatCorpseHelper.cs)); corpses persist in the room after successful flee ([`CombatEncounterLifecycle.RestoreExplorationAfterFlee`](../Scripts/3.Game/Combat/CombatEncounterLifecycle.cs)); validation mirrors trap salvage; inspect lines for **`CorpseFeature`**; tests ([`CombatCorpseHelperTests`](../Tests/TheDungeon.Tests/CombatCorpseHelperTests.cs)).

**Key files**

- [`MonsterDefinition.DeathLoot` / `HarvestDc`](../Scripts/0.Core/Monster/MonsterDefinition.cs), [`MonsterResource`](../Resources/MonsterResource.cs), [`MonsterMapper`](../Mappers/MonsterMapper.cs)
- [`CombatService.ExecutePlayerAttack`](../Scripts/3.Game/Services/CombatService.cs), [`CombatCorpseHelper`](../Scripts/3.Game/Services/CombatCorpseHelper.cs)
- [`ExplorationService`](../Scripts/3.Game/Services/ExplorationService.cs) (inspect — corpse lines)

## Phase 5 — Treasure unification

**Goal:** Align [`TreasureFeature`](../Scripts/0.Core/Treasure/TreasureFeature.cs) / [`TreasurePickupService`](../Scripts/3.Game/Services/TreasurePickupService.cs) with **`ChestFeature`** where payoff exceeds migration cost (gold piles vs inventory items documented).

### Migration boundary (Phase 5 decision)

We **keep a dual model** for now. Moving **gold piles** or **inspect-driven reveal** onto [`ChestFeature`](../Scripts/0.Core/Dungeon/ChestFeature.cs) would need new domain shapes (for example GP as loot rows or richer rules on [`LootableItemDefinition`](../Scripts/0.Core/Inventory/LootableItemDefinition.cs)), plus rewiring **Take** targeting, presenters, and tests. That cost exceeds Phase 5 scope.

| Concern | **Treasure** | **Chest / containers** |
|--------|--------------|-------------------------|
| **Payload** | [`TreasureInstance`](../Scripts/0.Core/Treasure/TreasureInstance.cs) + [`TreasureDefinition`](../Scripts/0.Core/Treasure/TreasureDefinition.cs) ([`TreasureKind`](../Scripts/0.Core/Enums/TreasureKind.cs): **`Currency`** or **`InventoryItem`**) | [`LootableItemDefinition`](../Scripts/0.Core/Inventory/LootableItemDefinition.cs) rows in `Contents` |
| **Currency** | `TreasureKind.Currency` grants a [`CoinPurse`](../Scripts/0.Core/Economy/CoinPurse.cs) in [`TreasurePickupService`](../Scripts/3.Game/Services/TreasurePickupService.cs) | Not representable today (items are id + quantity only) |
| **Discovery** | Per-instance `IsRevealed` + `DiscoverDc`; [`InspectService`](../Scripts/3.Game/Services/InspectService.cs) reveals hidden treasure | No hidden/reveal model on [`ContainerFeature`](../Scripts/0.Core/Dungeon/ContainerFeature.cs) |
| **Pickup UX** | **Take**: [`MainViewRoomSlots`](../Scripts/3.Game/Targeting/MainViewRoomSlots.cs), [`PlayerActionTargetResolvers`](../Scripts/3.Game/Targeting/PlayerActionTargetResolvers.cs), [`GameUiCoordinator`](../Scenes/MainUI/GameUiCoordinator.cs) | **Open container** by ordinal: [`RoomContainerLocator`](../Scripts/3.Game/Services/RoomContainerLocator.cs), [`ContainerLootInteractionService`](../Scripts/3.Game/Services/ContainerLootInteractionService.cs) |
| **Procedural rooms** | [`RoomFeaturePopulationService`](../Scripts/3.Game/Services/RoomFeaturePopulationService.cs) → [`RoomFeatureFactories.CreateTreasureFeature`](../Scripts/3.Game/Features/RoomFeatureFactories.cs) | Population **does** create [`ChestFeature`](../Scripts/0.Core/Dungeon/ChestFeature.cs) when [`PopulateableFeatureKind.Chest`](../Scripts/3.Game.Contracts/Dungeon/PopulateableFeatureKind.cs) is selected; contents come from [`ChestLootGenerator`](../Scripts/3.Game/Services/ChestLootGenerator.cs) |

**Treasure** remains responsible for: GP grants, hidden-then-revealed piles, and the current **Take** UI.

**ChestFeature** / **`ContainerFeature`** remains responsible for: explicit container loot (**items only**), ordinal resolution alongside salvage/corpse/chest, and the [`ContainerLootInteractionService`](../Scripts/3.Game/Services/ContainerLootInteractionService.cs) contract.

### Acceptable regressions (if we merge models later)

Any design that **drops per-pile reveal** without replacing it changes inspect difficulty and the fantasy of “finding” treasure—that is a **product** trade-off, not a free mechanical merge.

### Follow-up ideas (not committed)

- Extend loot rows (or a sibling type) to represent **GP in a chest** if gold-in-container is required.
- Route **inventory-only** treasure definitions through a chest-like feature **only** if paired with a clear reveal/hidden story (large UX and code change).

**Exit criteria**

- [x] Migration notes and acceptable regressions documented.

**Key files**

- [`TreasureFeature`](../Scripts/0.Core/Treasure/TreasureFeature.cs), [`TreasureDefinition`](../Scripts/0.Core/Treasure/TreasureDefinition.cs) / [`TreasureKind`](../Scripts/0.Core/Enums/TreasureKind.cs), [`TreasurePickupService`](../Scripts/3.Game/Services/TreasurePickupService.cs)
- [`InspectService`](../Scripts/3.Game/Services/InspectService.cs) (treasure discovery), [`RoomFeatureFactories`](../Scripts/3.Game/Features/RoomFeatureFactories.cs), [`RoomFeaturePopulationService`](../Scripts/3.Game/Services/RoomFeaturePopulationService.cs)
- [`ChestFeature`](../Scripts/0.Core/Dungeon/ChestFeature.cs), [`ContainerLootInteractionService`](../Scripts/3.Game/Services/ContainerLootInteractionService.cs), [`RoomContainerLocator`](../Scripts/3.Game/Services/RoomContainerLocator.cs)

---

## Procedural chests & balance hub (architecture)

- **`TreasureFeature` vs `ChestFeature`:** Procedural **treasure** (gold / inspect / Take) still comes from [`ITreasureDefinitionRepository`](../Scripts/0.Core/Treasure/TreasureDefinition.cs) and [`RoomFeatureFactories.CreateTreasureFeature`](../Scripts/3.Game/Features/RoomFeatureFactories.cs). **Item stacks** in a **chest** use [`ChestFeature`](../Scripts/0.Core/Dungeon/ChestFeature.cs) + [`ContainerLootInteractionService`](../Scripts/3.Game/Services/ContainerLootInteractionService.cs) for **Take** / loot-all. A room can have both if [`FeatureTypeRule`](../Scripts/3.Game.Contracts/Dungeon/FeatureTypeRule.cs) / **CannotCoexistWith** allows it.
- **`PopulateableFeatureKind.Chest`:** [`RoomFeaturePopulationService`](../Scripts/3.Game/Services/RoomFeaturePopulationService.cs) can place chests alongside other kinds. **What spawns** is only [`RoomFeaturePopulationParameters`](../Scripts/3.Game.Contracts/Dungeon/RoomFeaturePopulationParameters.cs) (feature mix). **How a chest is filled** is **not** on that type: use [`ChestLootGenerationParameters`](../Scripts/3.Game.Contracts/Dungeon/ChestLootGenerationParameters.cs) + [`ChestLootGenerator`](../Scripts/3.Game/Services/ChestLootGenerator.cs) (category weights, stack/quantity bounds) when `CreateChestFeature` runs.
- **Single [`GameBalanceSettingsResource`](../Resources/GameBalanceSettingsResource.cs):** Inspector-tunable **XP**, **floor generation** (room count, door chance, vertical exit weights), **feature rules** ([`FeaturePopulationRuleResource`](../Resources/FeaturePopulationRuleResource.cs) array), and **chest loot** (scalars + [`ChestLootCategoryWeightResource`](../Resources/ChestLootCategoryWeightResource.cs) rows). [`GameBalanceSettingsMapper`](../Mappers/GameBalanceSettingsMapper.cs) (host) maps the resource to **three** runtime DTOs: [`RoomFeaturePopulationParameters`](../Scripts/3.Game.Contracts/Dungeon/RoomFeaturePopulationParameters.cs), [`ChestLootGenerationParameters`](../Scripts/3.Game.Contracts/Dungeon/ChestLootGenerationParameters.cs), and [`FloorGenerationParameters`](../Scripts/3.Game.Contracts/Dungeon/FloorGenerationParameters.cs). [`GameRoot`](../Scenes/GameRoot.cs) builds [`DungeonBootstrap`](../Scripts/3.Game/Dungeon/DungeonBootstrap.cs) with pre-mapped hand-built + procedural floor factories.
- **Authoring:** Assign `GameBalanceSettings` on `GameRoot`. Add a **Chest** row to **Feature rules** and tune **Chest loot** category weights. Do not hand-edit `*.tres` in the agent; use the Godot Inspector per repo rules.

---

## Post-roadmap follow-ups (not committed)

**Take / main view — containers**

- Exploration **Take** lists treasure plus [**LootContainerAll**](../Scripts/3.Game/Targeting/TargetPayload.cs) targets for each **lootable** [`ContainerFeature`](../Scripts/0.Core/Dungeon/ContainerFeature.cs) (ordinal matches [`RoomContainerLocator`](../Scripts/3.Game/Services/RoomContainerLocator.cs)); [`MainViewRoomSlots`](../Scripts/3.Game/Targeting/MainViewRoomSlots.cs) locators and [`MainViewPresentationBuilder`](../Scripts/4.UI/Presentation/MainViewPresentationBuilder.cs) draw one row per container with highlight `container:N`.
- **Godot:** [`IconResolver`](../Scenes/IconResolver.cs) logs once for `mainview/container/*` keys until you wire textures (extend resolver or add resource-backed icons—no `*.tscn` edits required for wiring resolver code).

### Deferred epics (product-gated)

- **GP / treasure → chest:** See Phase 5 “Follow-up ideas” ([GP-in-container representation](../Scripts/0.Core/Inventory/LootableItemDefinition.cs), inventory treasure → chest + reveal story).
- Procedural **chests** with item loot: **implemented** — see [Procedural chests & balance hub](#procedural-chests--balance-hub-architecture).
