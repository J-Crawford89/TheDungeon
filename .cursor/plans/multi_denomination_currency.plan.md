---
name: Multi-denomination currency (CoinPurse)
overview: Four coin types at 100:1 steps; CoinPurse holds four stacks (no auto roll-up). Rewards carry CoinPurse grants. TotalEquivalentCopper for wealth/comparison; HUD and notebook show one label per denomination when count ≥ 1. Legacy tres numbers user-owned; agent edits C# only.
todos:
  - id: core-coinpurse
    content: CoinPurse + CurrencyMath (TotalEquivalentCopper, purse merge on grant); PlayerState; snapshot/reset/game-over; tests
    status: completed
  - id: grants-treasure-bg-items
    content: TreasureDefinition + Resource + mapper CoinPurse grant; TreasurePickupService; CharacterBackground CoinPurse start; Item ValueInCopper + mapper (no .tres edits)
    status: completed
  - id: ui-character-notebook
    content: CharacterPanel four denomination Label exports + Render visibility rules; NotebookOverlay Inventory page four Label exports + refresh wiring
    status: completed
  - id: narrative-docs-tests
    content: NarrativeService pickup lines; CharacterCreationScreen copy; docs/CURRENCY.md; dotnet test
    status: completed
isProject: false
---

# Multi-denomination currency — CoinPurse model

## Exchange math (fixed)

| Step | Relation |
|------|----------|
| 100 Copper | 1 Silver |
| 100 Silver | 1 Gold → 10,000 Copper equivalent |
| 100 Gold | 1 Platinum → 1,000,000 Copper equivalent |

## Storage model

**`CoinPurse`**: four non-negative integers — **Copper**, **Silver**, **Gold**, **Platinum**. Physical stacks; **no automatic conversion** when any stack exceeds 99 (186 Copper stays 186 Copper).

**`CurrencyMath.TotalEquivalentCopper(CoinPurse)`** (and adding two purses / comparing price to purse):

- Use for **total wealth**, **afford checks**, **sorting**, **value comparisons** expressed as a single scalar when helpful.
- Does **not** mutate the purse.
- UI may derive display from raw stacks **or** from formatted lines — comparisons can stay copper-equivalent internally.

**Future:** bank/exchange NPC reshuffles denominations at 100:1 without changing `TotalEquivalentCopper`; **out of scope** for this milestone unless stubbing helpers.

---

## Grants (locked)

Any reward that grants currency carries a **`CoinPurse`** on the definition (domain + serialized resource fields mapped to the same shape). Pickup/start logic **adds** that purse to the player’s purse stack-wise (component-wise addition). No silent normalization.

Treasure / background / any future quest reward should use this pattern so authors control **exactly which coins** drop.

---

## Legacy authoring + `.tres` policy (locked)

- Existing **gold-ish numbers were placeholders**, closer to **copper-scale** in spirit; balance will be tweaked in content.
- **Agent does not edit `*.tres`** per repo rules; **you** adjust exported values after C# export renames / new CoinPurse fields are available.
- **Tests:** update so they compile and reflect `CoinPurse` / new APIs; numeric expectations can stay aligned with **current placeholder magnitudes** until you retune content.

---

## HUD + Notebook UI (locked)

### Main HUD — [`CharacterPanel`](e:\DevProjects\Godot\the-dungeon\Scenes\MainUI\CharacterPanel.cs)

- Replace the single **`_goldLabel`** pattern with **four exported `Label?` fields** (e.g. `_copperCoinLabel`, `_silverCoinLabel`, `_goldCoinLabel`, `_platinumCoinLabel`) — names can match team convention.
- **`Render` logic:** for each denomination, **if count ≥ 1**: set label visible, text = formatted line for that type + count; **if count == 0**: hide label (or clear + hide).
- **Layout / styling:** you rework the scene in Godot; agent only wires exports + visibility/text rules.

### Notebook — Inventory tab — [`NotebookOverlay`](e:\DevProjects\Godot\the-dungeon\Scenes\Menus\NotebookOverlay.cs)

- Add **four exported labels** on the **inventory page** (same rule: show only denominations with ≥ 1 coin).
- Refresh whenever inventory notebook refresh runs (extend nested **`InventoryNotebookCoordinator`** in [`NotebookOverlay.cs`](e:\DevProjects\Godot\the-dungeon\Scenes\Menus\NotebookOverlay.cs) / **`RefreshAll`** path so opening inventory / purse changes updates coin lines).

---

## Domain / pipeline touchpoints

- [`PlayerState`](e:\DevProjects\Godot\the-dungeon\Scripts\2.State\Player\PlayerState.cs): `Gold` → **`CoinPurse`**.
- [`PlayerCharacterSnapshot`](e:\DevProjects\Godot\the-dungeon\Scripts\2.State\Player\PlayerCharacterSnapshot.cs) / [`FallenAdventurerRecord`](e:\DevProjects\Godot\the-dungeon\Scripts\2.State\Player\FallenAdventurerRecord.cs): four fields or nested purse.
- [`TreasureDefinition`](e:\DevProjects\Godot\the-dungeon\Scripts\0.Core\Treasure\TreasureDefinition.cs) / [`TreasureResource`](e:\DevProjects\Godot\the-dungeon\Resources\TreasureResource.cs): **`CoinPurse Grant`** (name TBD) for currency-kind treasures; retire **`ValueInGp`** / misleading **`TreasureKind.Gold`** naming in favor of currency grant kind + purse.
- [`TreasurePickupService`](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Services\TreasurePickupService.cs): apply purse to player.
- [`CharacterBackgroundDefinition`](e:\DevProjects\Godot\the-dungeon\Scripts\0.Core\World\CharacterBackgroundDefinition.cs) / Resource / mapper: **`StartingCoinPurse`** (or four ints).
- [`CharacterCreationService`](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Services\CharacterCreationService.cs), [`CharacterCreationScreen`](e:\DevProjects\Godot\the-dungeon\Scenes\Menus\CharacterCreationScreen.cs): summary text.
- [`ItemDefinition`](e:\DevProjects\Godot\the-dungeon\Scripts\0.Core\Item\ItemDefinition.cs) **`ValueInGold` → `ValueInCopper`** (equivalent for sell/compare); [`ItemMapper`](e:\DevProjects\Godot\the-dungeon\Mappers\ItemMapper.cs).
- [`NarrativeService.GameOverAndItems`](e:\DevProjects\Godot\the-dungeon\Scripts\3.Game\Narrative\NarrativeService.GameOverAndItems.cs): pickup lines for granted purse / optional equivalent copper mention.
- [`GameUiCoordinator`](e:\DevProjects\Godot\the-dungeon\Scenes\MainUI\GameUiCoordinator.cs) / refresh flags: ensure notebook + character panel refresh when currency changes (same as today for gold if applicable).

---

## Implementation phases

1. **Core:** `CoinPurse`, `CurrencyMath`, player state + snapshot + reset.
2. **Grants:** treasure + background + pickup; mapper/resource C# (you fix `.tres` in editor).
3. **Items:** `ValueInCopper` + mapper only.
4. **UI:** CharacterPanel + NotebookOverlay labels + refresh wiring.
5. **Narrative + docs:** `docs/CURRENCY.md`, tests, `dotnet test`.

---

## Non-goals (this milestone)

- Bank / merchant **exchange** UI.
