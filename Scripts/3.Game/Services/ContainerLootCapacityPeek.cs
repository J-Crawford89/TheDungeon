#nullable enable
using System.Collections.Generic;
using System.Linq;

/// <summary>Lightweight backpack crowding hints; a full picker refines UX in Phase 3.</summary>
public static class ContainerLootCapacityPeek
{
	/// <summary>True when unequipped backpack rows already exceed what the UI grid can show.</summary>
	public static bool BackpackGridShowsOverflow(InventoryState inventory, int maxUnequippedRows)
	{
		var (_, overflow) = InventoryBackpackGridModel.BuildUnequippedRows(inventory, maxUnequippedRows);
		return overflow;
	}

	/// <summary>
	/// Rough signal: true when current unequipped row count plus new distinct item ids from <paramref name="additions"/>
	/// (that are not already full stacks in inventory) exceeds <paramref name="maxUnequippedRows"/>.
	/// </summary>
	public static bool LikelyCrowdedAfterTake(
		InventoryState inventory,
		IReadOnlyList<LootableItemDefinition> additions,
		IItemDefinitionRepository items,
		int maxUnequippedRows)
	{
		if (BackpackGridShowsOverflow(inventory, maxUnequippedRows))
			return true;

		var fullRows = InventoryBackpackGridModel.BuildUnequippedRows(inventory, int.MaxValue).VisibleRows;
		var rowCount = fullRows.Count;
		var seenNewKind = new HashSet<string>();
		foreach (var add in additions)
		{
			var id = add.ItemDefinitionId.Trim();
			if (id.Length == 0 || add.Quantity <= 0)
				continue;
			var def = items.TryGetById(id);
			if (def == null)
				continue;

			var existing = fullRows.FirstOrDefault(r => r.Definition.Id == def.Id);
			var cap = System.Math.Max(1, def.MaxStackSize);
			if (existing != null && existing.Quantity + add.Quantity <= cap)
				continue;

			if (existing != null && existing.Quantity < cap)
				continue;

			if (!seenNewKind.Contains(def.Id))
			{
				seenNewKind.Add(def.Id);
				rowCount++;
			}
		}

		return rowCount > maxUnequippedRows;
	}
}
