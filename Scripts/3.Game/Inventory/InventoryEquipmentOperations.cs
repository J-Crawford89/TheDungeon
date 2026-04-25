#nullable enable
using System;
using System.Collections.Generic;

/// <summary>Equip and unequip rules for <see cref="InventoryState"/> (notebook / game logic).</summary>
public static class InventoryEquipmentOperations
{
	/// <summary>Equips one unit from a backpack row. Uses the first empty slot in <see cref="EquipmentDefinition.Slots"/> order, or swaps into the first slot in that list if all are occupied.</summary>
	public static bool TryEquipOneFromBackpackRow(InventoryState inventory, ItemInstance backpackRow)
	{
		ArgumentNullException.ThrowIfNull(backpackRow);

		if (backpackRow.Definition is not EquipmentDefinition eqDef)
			return false;
		if (eqDef.Slots == null || eqDef.Slots.Count == 0)
			return false;

		var rowIndex = inventory.Items.FindIndex(i => i.InstanceId == backpackRow.InstanceId);
		if (rowIndex < 0 || backpackRow.Quantity <= 0)
			return false;

		var targetSlot = PickEquipSlot(inventory, eqDef.Slots);
		if (targetSlot == null)
			return false;

		ItemInstance instanceToEquip;
		var row = inventory.Items[rowIndex];
		if (row.Quantity > 1)
		{
			row.Quantity--;
			instanceToEquip = new ItemInstance
			{
				Definition = row.Definition,
				Quantity = 1,
			};
			inventory.Items.Add(instanceToEquip);
		}
		else
			instanceToEquip = row;

		inventory.EquippedBySlot[targetSlot.Value] = instanceToEquip;
		return true;
	}

	private static EquipmentSlot? PickEquipSlot(InventoryState inventory, List<EquipmentSlot> slots)
	{
		foreach (var s in slots)
		{
			if (!inventory.EquippedBySlot.TryGetValue(s, out var cur) || cur == null)
				return s;
		}

		return slots.Count > 0 ? slots[0] : null;
	}

	public static bool TryUnequipSlot(InventoryState inventory, EquipmentSlot slot)
	{
		if (!inventory.EquippedBySlot.TryGetValue(slot, out var cur) || cur == null)
			return false;

		inventory.EquippedBySlot[slot] = null;
		return true;
	}
}
