# Loot, containers, and phased delivery

Single checklist for container-backed loot. Reference in chats: `@docs/LOOT_CONTAINERS_ROADMAP.md`.

## Scope boundaries

- **In-editor:** New `[Export]` arrays on resources are wired in the Godot Inspector (agent does not edit `*.tscn` / serialized `*.tres` per repo rules).
- **Runtime:** Domain types in `Scripts/0.Core`; behavior in `Scripts/3.Game`; Godot `Resource` subclasses under `Resources/`; mappers under `Mappers/`.

## Phase 1 — Foundation (current)

**Goal:** `LootableItemDefinition`, `LootableItemResource`, mappers, `ContainerFeature` hierarchy (`SalvageFeature`, `CorpseFeature`, `ChestFeature`), `InventoryState.AddOrStack` for quantities, `ContainerLootOperations` + narrative lines, capacity peek helper, unit tests.

**Exit criteria**

- [ ] Types and services compile; `dotnet test` green.
- [ ] Transfer from container to inventory is **explicit** (no silent backpack fills from disarm—that remains Phase 2).

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

- [ ] Disarm tests updated ([`TrapServiceDisarmTests`](../Tests/TheDungeon.Tests/TrapServiceDisarmTests.cs)).

## Phase 3 — Picker contract

**Goal:** UI-agnostic API + DTOs (`open container`, `loot selected`, `loot all`, `close`); optional stub implementation.

**Exit criteria**

- [ ] Presenter/tests can drive loot without Godot scenes.

## Phase 4 — Monsters

**Goal:** On death → **`CorpseFeature`** with loot from monster definition; room presence / flee rules when ready.

**Exit criteria**

- [ ] Dead monsters leave loot only while rules allow.

## Phase 5 — Treasure unification

**Goal:** Align [`TreasureFeature`](../Scripts/0.Core/Treasure/TreasureFeature.cs) / [`TreasurePickupService`](../Scripts/3.Game/Services/TreasurePickupService.cs) with **`ChestFeature`** where payoff exceeds migration cost (gold piles vs inventory items documented).

**Exit criteria**

- [ ] Migration notes and acceptable regressions documented.
