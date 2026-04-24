#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Builds backpack grid rows from <see cref="InventoryState.Items"/> excluding instances equipped in <see cref="InventoryState.EquippedBySlot"/>.</summary>
public static class InventoryBackpackGridModel
{
	/// <param name="maxSlots">Grid capacity (e.g. 16).</param>
	/// <returns>Visible rows in <see cref="Items"/> order after filter, at most <paramref name="maxSlots"/> entries; <see cref="Overflow"/> when more unequipped stacks existed.</returns>
	public static (IReadOnlyList<ItemInstance> VisibleRows, bool Overflow) BuildUnequippedRows(InventoryState state, int maxSlots)
	{
		if (maxSlots < 0)
			throw new ArgumentOutOfRangeException(nameof(maxSlots));

		var equippedIds = new HashSet<Guid>();
		foreach (var inst in state.EquippedBySlot.Values)
		{
			if (inst != null)
				equippedIds.Add(inst.InstanceId);
		}

		var unequipped = new List<ItemInstance>();
		foreach (var row in state.Items)
		{
			if (!equippedIds.Contains(row.InstanceId))
				unequipped.Add(row);
		}

		var overflow = unequipped.Count > maxSlots;
		if (overflow)
			unequipped = unequipped.Take(maxSlots).ToList();

		return (unequipped, overflow);
	}
}
