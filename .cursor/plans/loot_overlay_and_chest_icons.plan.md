---
name: Loot overlay and chest icons
overview: Wire ContainerLootOverlay to ContainerLootInteractionService with fresh row instances each open and clear teardown on close; add ChestIconsResource plus SalvageIcon; per-monster corpse icons via MonsterResource.CorpseIcon with CorpseFeature lineage; combat Take excludes containers; simplify input-guard expectations.
---

# Container loot overlay + main-view icons (revised)

## Part A — Resources and corpse icons

### Chest / salvage aggregation resource

Add **`ChestIconsResource`** (`[GlobalClass]` under [`Resources/`](e:\DevProjects\Godot\the-dungeon\Resources\)), parallel to [**`MainViewTraversalIcons`**](e:\DevProjects\Godot\the-dungeon\Resources\MainViewTraversalIcons.cs):

- **`ChestIcon`** / **`ChestLockedIcon`** — map to presentation ids **`chest`** and **`chest_locked`** (fallback locked → unlocked if null).
- **`SalvageIcon`** — map to **`salvage`**.
- **Do not** put corpse art here; corpse icons are **per monster** on **`MonsterResource`**.

### Per-monster corpse icons

- On [**`MonsterResource`**](e:\DevProjects\Godot\the-dungeon\Resources\MonsterResource.cs), add **`[Export] Texture2D? CorpseIcon`**. Resolver prefers **`CorpseIcon`** when present for that monster’s corpse; optional fallback chain (e.g. **`CorpseIcon` → living **`Icon`** → null).

### Domain + keys so the resolver knows which monster

- Extend [**`CorpseFeature`**](e:\DevProjects\Godot\the-dungeon\Scripts\0.Core\Dungeon\CorpseFeature.cs) with **`SourceMonsterDefinitionId`** (string, set when spawned). [**`CombatCorpseHelper.SpawnCorpseOnMonsterDeath`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Services\CombatCorpseHelper.cs) assigns **`definition.Id`**.
- Update [**`PresentationIconKeys.MainView.ForContainer`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Presentation\PresentationIconKeys.cs): **`CorpseFeature`** → key that carries the monster id (e.g. **`mainview/container/corpse/{monsterId}`** — [**`IconResolver`**](e:\DevProjects\Godot\the-dungeon\Scenes\IconResolver.cs) already passes the **`id`** segment after the first slash; supporting an id like **`corpse/rat`** means **`ResolveContainer`** receives **`corpse/rat`**, splits on **`/`**, resolves **`corpse`** branch, loads **`MonsterResource`** by **`rat`**, uses **`CorpseIcon`**).

### IconResolver + GameRoot

- Extend **`IconResolver`** ctor with **`ChestIconsResource?`**; implement **`ResolveContainer`** for **`chest`**, **`chest_locked`**, **`salvage`**, and corpse ids (per-monster via **`MonsterResourceDatabase`**).
- [**`GameRoot`**](e:\DevProjects\Godot\the-dungeon\Scenes\GameRoot.cs): **`[Export] ChestIconsResource?`** and pass into **`IconResolver`** (same pattern as **`MainViewTraversalIcons`**).

### Editor

- Create and assign **`ChestIcons.tres`** + assign monster **`CorpseIcon`** textures in Inspector (**no agent `.tres` text edits**).

---

## Part B — Loot overlay behavior

### Population and lifecycle

- **Placeholder rows**: The five **`LootableItemControl`** instances under **`LootGrid`** in [**`main_ui.tscn`**](e:\DevProjects\Godot\the-dungeon\Scenes\MainUI\main_ui.tscn) are **layout helpers only**. Implementation **clears `LootGrid`** on each **`Open`**, **`Instantiate`**s one **`LootableItemControl`** per **`ContainerLootStackRowDto`** (PackedScene reference), binds data, adds as child.
- **Close / teardown**: On close (and before rebuild on reopen), **remove all dynamically added children** (`QueueFree` / clear). **No caching** of row controls or bound DTOs across opens — always rebuild from **`TryBuildPanel`** so stale data cannot resurface.

### Title and subtitle

- **Title**: Container **type** — **Chest**, **Corpse**, or **Salvage** (from **`ContainerLootKind`** / friendly mapping).
- **Subtitle**: **Source** — trap display name for salvage (e.g. **Snare Trap**), monster display name for corpse (e.g. **Rat**). **Chest: blank** for now.

This likely requires extending [**`ContainerLootPanelDto`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game.Contracts\Inventory\ContainerLootPanelDto.cs) (and [**`TryBuildPanel`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Services\ContainerLootInteractionService.cs)) with explicit **`PanelSubtitle`** (and possibly **`PanelTitle`**) populated from container subtype + optional lineage. [**`SalvageFeature`**](e:\DevProjects\Godot\the-dungeon\Scripts\0.Core\Dungeon\SalvageFeature.cs) currently has **no trap reference** — if subtitle must show **“Snare Trap”**, add minimal authoring on **`SalvageFeature`** (e.g. **`SourceTrapDefinitionId`**) when salvage is spawned from [**`TrapService`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Services\TrapService.cs), then resolve the display name via **`ITrapDefinitionRepository`**. **Corpse** subtitle uses the monster name from **`CorpseFeature.SourceMonsterDefinitionId`** via **`IMonsterDefinitionRepository`** / item repo as appropriate. **Chest** subtitle remains **blank** for now.

### Interaction

- **Dimmer**: **No** close-on-click; dimmer still blocks pointer to the game behind the overlay.
- **Warning label**: Keep **`LikelyCrowdedAfterTakeAll`** on **`LootWarning`** as today.

### Scripts (C#)

- **`LootableItemControl`**: partial class for icon, name, quantity, selection state.
- **Overlay controller**: open/close, populate grid, **`Take All` / `Take Selected` / `Close`** wired to **`ContainerLootInteractionService`**; refresh HUD after successful transfer.

---

## Part C — Take routing and combat rules

### 4 — Combat: treasure only (no containers)

- In [**`PlayerActionTargetResolvers.BuildFlatTakeTargets`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Targeting\PlayerActionTargetResolvers.cs), when **`mode == DungeonMode.Combat`**, **omit** all **`ContainerFeature`** **`LootContainerAll`** descriptors. Players may only take **loose treasure** instances in combat (existing treasure rows), not chests/salvage/corpses.
- **Corollary**: Loot overlay is **exploration-only** for containers; **`CombatService.ExecutePlayerTakeTreasure`** does not need container/overlay paths for **`LootContainerAll`** once combat never emits that payload. Simplify plan: **no combat-specific overlay completion** or **`AdvanceTurn`** branches for container loot — remove that complexity from the earlier draft.

### 5 — Coordinator “overlay open” guard

- **Dropped**: Do **not** add **`IsContainerLootOverlayOpen`** checks to **`GameUiCoordinator.CanUseExplorationCommands`**. Full-screen overlay + modal input handling means **no click-through** to commands; no extra coordinator flag required.

### Exploration flow

- **`LootContainerAll`** from [**`ResolveTakeTargets`**](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Targeting\PlayerActionTargetResolvers.cs) (exploration only, after combat filter) opens the overlay; **`TryBuildPanel`** drives UI; transfers call **`TryLootAll`** / **`TryLootSelected`** then refresh HUD and close or rebuild.

---

## Verification

- **`dotnet test`** — update any tests that assume corpses without **`SourceMonsterDefinitionId`**; add resolver/corpse key tests if host tests exist; adjust take-target tests so combat lists exclude containers.

---

## Implementation todos

1. **ChestIconsResource + IconResolver + GameRoot**; **MonsterResource.CorpseIcon**; **CorpseFeature** id + **PresentationIconKeys** + **MainViewRoomSlots** if key format changes; **CombatCorpseHelper** sets id.
2. **ContainerLootPanelDto** subtitle/title fields + **TryBuildPanel** fill rules (chest subtitle blank).
3. **LootableItemControl** + overlay controller; **MainUi** wiring; clear grid lifecycle.
4. **PlayerActionTargetResolvers** combat filter for containers; **ExplorationUiPresenter** overlay opener for **`LootContainerAll`** only.
5. **Tests + dotnet test**.
