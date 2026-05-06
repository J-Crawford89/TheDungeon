---
name: Open containers UX
overview: Open button vs Take, WasOpened state, main-view icons—corpse opened art lives on MonsterResource; chest/salvage opened art on ChestIconsResource.
---

# Open containers + opened-state visuals + Take scope

## Goals

1. **Opened indication** — After the player has opened a container’s loot UI at least once, the main-view icon should reflect “opened.”
2. **Take** — Only **loose treasure** (`TreasureFeature`). Containers must **not** use Take.
3. **Open** — New exploration-only command; one container → open immediately; multiple → targeting like Take.

Salvage that becomes empty continues to remove the feature when empty.

---

## 1. Domain: “has been opened”

- Add **`bool WasOpened { get; set; }`** on **`ContainerFeature`** (default `false`).
- Set **`WasOpened = true`** when the loot panel successfully opens (**`ContainerLootOverlay`** after **`TryBuildPanel`** succeeds), via **`RoomContainerLocator.TryGetNthContainer`**.

---

## 2. Presentation keys + assets

### Chest and salvage (global resources)

- Extend **`ChestIconsResource`** with exports such as **`ChestOpenedIcon`**, **`SalvageOpenedIcon`** (and locked variants if needed). Map new **`ResolveContainer`** id segments (e.g. `chest_opened`, `salvage_opened`).

### Corpse (per monster — **confirmed**)

- Add a **second texture on [`MonsterResource`](e:\DevProjects\Godot\the-dungeon\Resources\MonsterResource.cs)** for the opened state, e.g. **`[Export] Texture2D? CorpseOpenedIcon`**, with fallback **`CorpseOpenedIcon ?? CorpseIcon ?? Icon`** in **`IconResolver`** when resolving corpse keys for a monster id and **`WasOpened`** is true.

- Extend **`PresentationIconKeys.MainView.ForContainer`** so **`CorpseFeature`** uses a distinct key when **`WasOpened`** (e.g. still under `mainview/container/corpse/...` with an opened variant, or a dedicated segment like `corpse_opened/{id}`) — **`IconResolver.ResolveContainer`** branches on that and loads **`MonsterResource`** for **`CorpseOpenedIcon`**.

### Locked chests

- **`ChestFeature.Locked`** continues to use locked art until unlocked; opened-state art applies once the player can open and **`WasOpened`** is set.

- **`MainViewRoomSlots`** keeps calling **`ForContainer(cf)`** — no slot-order changes.

**Editor:** Assign textures on **`ChestIconsResource`**, **`MonsterResource`** (corpse opened), per repo rules (no agent `.tres` text edits).

---

## 3. Take: treasure only

- **`PlayerActionTargetResolvers.BuildFlatTakeTargets`** — Remove **`case ContainerFeature`** (no **`LootContainerAll`** from Take).

- **`GameUiCoordinator.RefreshHud`** — **`ApplyTakeButtonVisible`** uses only **`TreasurePickupService.HasTakeableLootInCurrentRoom`** (drop **`RoomContainerLocator.CurrentRoomHasLootableContainers`** from Take visibility).

---

## 4. Open: targeting + coordinator + presenter

- **`PlayerActionTargetResolvers.ResolveOpenContainerTargets(session, mode)`** — Exploration only; list every **`ContainerFeature`** (same ordinal as **`RoomContainerLocator`** / **`MainViewRoomSlots`**), including empty chest/corpse. Payload: **`LootContainerAll`** + **`ContainerOrdinal`**.

- **`GameUiCoordinator`**: **`OnOpenPressed`**, **`ApplyOpenButtonVisible(exploration && CurrentRoomHasAnyContainer)`**, helper **`RoomContainerLocator.CurrentRoomHasAnyContainer`**.

- **`ExplorationUiPresenter`**: Dedicated handler for opening overlay from **`LootContainerAll`** (Take path never sends it).

- **`CommandPanel`**: **`OpenPressed`**, **`ApplyOpenButtonVisible`**.

- **`MainUi`**: Wire **`OpenPressed`**.

**Editor:** Add **Open** button to command UI and export on **`CommandPanel`** (agent does not edit `*.tscn`).

---

## 5. Tests

- Resolver tests: Take excludes containers; Open lists containers; combat behavior unchanged where applicable.
- **`dotnet test`**.

---

## Implementation todos

1. **domain-wasopened** — `ContainerFeature.WasOpened`; set in `ContainerLootOverlay` on successful open.
2. **icons-keys-resolver** — `PresentationIconKeys` + `ChestIconsResource` (chest/salvage opened) + `MonsterResource.CorpseOpenedIcon` + `IconResolver` corpse opened branch.
3. **take-open-split** — Take resolver + coordinator Take visibility; `ResolveOpenContainerTargets` + `CurrentRoomHasAnyContainer`.
4. **command-ui-wire** — `CommandPanel` + coordinator + `MainUi` + `ExplorationUiPresenter`.
5. **tests** — Update/add tests; `dotnet test`.
