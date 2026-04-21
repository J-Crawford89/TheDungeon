---
name: Inventory and potions
overview: Same as Cursor plan — no snapshot field for potion/inventory counts; CommandPanel uses live InventoryState (+ optional helper).
---

Synced with **`inventory_and_potions_d343d27b.plan.md`** in Cursor plans.

**§6 gist:** Remove **`PlayerState.HealthPotionCount`**. **`PlayerCharacterSnapshot`** gets **no** redundant inventory quantity fields. **`CommandPanel`** **`PotionPressed`** / **`Disabled`** reads **`InventoryState.Items`** at refresh (sum **`Quantity`** where **`Definition.Id`** == **`InventoryIds.HealthPotionItemId`**). Prefer a **small helper** on **`InventoryState`** or **`PlayerState`** (`GetInventoryQuantity(itemId)`) — computed only, not snapshotted. Rewire UI refresh if it currently depends on snapshot for potion counts.
