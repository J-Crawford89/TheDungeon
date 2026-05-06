# Currency (CoinPurse)

## Model

- **Coin types:** Copper, Silver, Gold, Platinum.
- **Rates:** 100 Copper = 1 Silver; 100 Silver = 1 Gold (10,000 Copper); 100 Gold = 1 Platinum (1,000,000 Copper equivalent).
- **`CoinPurse`** ([`Scripts/0.Core/Economy/CoinPurse.cs`](../Scripts/0.Core/Economy/CoinPurse.cs)): four non-negative counts. Coins **do not auto-consolidate** across denominations (e.g. 150 Copper stays 150 Copper until a future exchange/bank flow).
- **`CurrencyMath.TotalEquivalentCopper`** ([`CurrencyMath.cs`](../Scripts/0.Core/Economy/CurrencyMath.cs)): scalar for wealth comparisons and afford checks; does not change the purse.

## Authoring

- **Treasure:** [`TreasureDefinition.CurrencyGrant`](../Scripts/0.Core/Treasure/TreasureDefinition.cs) + [`TreasureKind.Currency`](../Scripts/0.Core/Enums/TreasureKind.cs); [`TreasureResource.CurrencyGrant`](../Resources/TreasureResource.cs) uses [`CoinPurseResource`](../Resources/CoinPurseResource.cs) in the Godot inspector.
- **Starting wealth:** [`CharacterBackgroundDefinition.StartingCoinPurse`](../Scripts/0.Core/World/CharacterBackgroundDefinition.cs) / [`CharacterBackgroundResource.StartingCoinPurse`](../Resources/CharacterBackgroundResource.cs).
- **Item value:** [`ItemDefinition.ValueInCopper`](../Scripts/0.Core/Item/ItemDefinition.cs) (sell/compare baseline in copper equivalent).

Serialized `.tres` updates after export renames are **editor-owned** (see workspace rules).

## UI

- [`CharacterPanel`](../Scenes/MainUI/CharacterPanel.cs): optional exported labels per denomination; [`CoinPurseLabelHelper`](../Scenes/CoinPurseLabelHelper.cs) shows a line only when count ≥ 1.
- [`NotebookOverlay`](../Scenes/Menus/NotebookOverlay.cs): same pattern on the inventory tab; refreshed with inventory refresh.
