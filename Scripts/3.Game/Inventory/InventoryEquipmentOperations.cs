#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Equip and unequip rules for <see cref="InventoryState"/> (notebook / game logic).</summary>
public static class InventoryEquipmentOperations
{
	/// <summary>True when the player must pick among <see cref="SingleSlotEquipChoices"/> (no multi-slot footprint, multiple distinct <see cref="EquipmentDefinition.Slots"/>).</summary>
	public static bool RequiresPlayerSlotChoiceForEquip(EquipmentDefinition eqDef)
	{
		if (FootprintSlots(eqDef).Count > 0)
			return false;

		return DistinctSlotsPreserveOrder(eqDef.Slots ?? []).Count > 1;
	}

	/// <summary>Distinct single-slot targets in first-seen order; empty when the item uses a multi-slot <see cref="EquipmentDefinition.OccupiedSlots"/> footprint.</summary>
	public static IReadOnlyList<EquipmentSlot> SingleSlotEquipChoices(EquipmentDefinition eqDef)
	{
		if (FootprintSlots(eqDef).Count > 0)
			return Array.Empty<EquipmentSlot>();

		return DistinctSlotsPreserveOrder(eqDef.Slots ?? []);
	}

	/// <summary>
	/// Auto-equip when there is no ambiguity: single-slot item with one distinct <see cref="EquipmentDefinition.Slots"/> entry,
	/// or any item with a non-empty <see cref="EquipmentDefinition.OccupiedSlots"/> footprint.
	/// Returns <see langword="false"/> when the player must pick a slot (multiple <see cref="EquipmentDefinition.Slots"/>); use <see cref="TryEquipOneFromBackpackRowToSlot"/>.
	/// </summary>
	public static bool TryEquipOneFromBackpackRow(InventoryState inventory, ItemInstance backpackRow)
	{
		if (backpackRow.Definition is not EquipmentDefinition eqDef)
			return false;

		if (HasMultiSlotFootprint(eqDef))
			return TryEquipOneFromBackpackRowToFootprint(inventory, backpackRow, eqDef);

		var candidates = DistinctSlotsPreserveOrder(eqDef.Slots);
		if (candidates.Count == 0)
			return false;
		if (candidates.Count > 1)
			return false;

		return TryEquipOneFromBackpackRowToSlot(inventory, backpackRow, candidates[0]);
	}

	/// <summary>
	/// Character-creation auto-equip for single-slot equipment:
	/// tries first empty allowed slot; if occupied, displaces only when the occupant can move to another empty allowed slot.
	/// Returns <see langword="false"/> when not equipable under these rules (including multi-slot footprint items).
	/// </summary>
	public static bool TryAutoEquipStartingGearOne(InventoryState inventory, ItemInstance backpackRow)
	{
		ArgumentNullException.ThrowIfNull(backpackRow);

		if (backpackRow.Definition is not EquipmentDefinition eqDef)
			return false;
		if (HasMultiSlotFootprint(eqDef))
			return false;

		var allowed = DistinctSlotsPreserveOrder(eqDef.Slots ?? []);
		if (allowed.Count == 0)
			return false;

		// First preference: first empty allowed slot.
		foreach (var slot in allowed)
		{
			if (!inventory.EquippedBySlot.TryGetValue(slot, out var occupant) || occupant == null)
				return TryEquipOneFromBackpackRowToSlot(inventory, backpackRow, slot);
		}

		// No empty target slot for the new item; try to move an existing occupant.
		foreach (var blockedSlot in allowed)
		{
			if (!inventory.EquippedBySlot.TryGetValue(blockedSlot, out var occupant) || occupant == null)
				continue;
			if (occupant.Definition is not EquipmentDefinition occupiedDef)
				continue;
			if (HasMultiSlotFootprint(occupiedDef))
				continue;

			var occupiedAllowed = DistinctSlotsPreserveOrder(occupiedDef.Slots ?? []);
			foreach (var occupiedCandidateSlot in occupiedAllowed)
			{
				if (occupiedCandidateSlot == blockedSlot)
					continue;
				if (inventory.EquippedBySlot.TryGetValue(occupiedCandidateSlot, out var occupiedCandidate) && occupiedCandidate != null)
					continue;

				inventory.EquippedBySlot[occupiedCandidateSlot] = occupant;
				inventory.EquippedBySlot[blockedSlot] = null;
				return TryEquipOneFromBackpackRowToSlot(inventory, backpackRow, blockedSlot);
			}
		}

		return false;
	}

	/// <summary>Single-slot equip to a slot listed in <see cref="EquipmentDefinition.Slots"/>; not for multi-slot <see cref="EquipmentDefinition.OccupiedSlots"/> items.</summary>
	public static bool TryEquipOneFromBackpackRowToSlot(InventoryState inventory, ItemInstance backpackRow, EquipmentSlot chosenSlot)
	{
		ArgumentNullException.ThrowIfNull(backpackRow);

		if (backpackRow.Definition is not EquipmentDefinition eqDef)
			return false;
		if (HasMultiSlotFootprint(eqDef))
			return false;
		if (eqDef.Slots == null || eqDef.Slots.Count == 0)
			return false;

		var allowed = DistinctSlotsPreserveOrder(eqDef.Slots);
		if (!allowed.Contains(chosenSlot))
			return false;

		var rowIndex = inventory.Items.FindIndex(i => i.InstanceId == backpackRow.InstanceId);
		if (rowIndex < 0 || backpackRow.Quantity <= 0)
			return false;

		if (!TrySplitStackForEquip(inventory, rowIndex, out var instanceToEquip))
			return false;

		ClearConflictsForSlots(inventory, [chosenSlot]);
		inventory.EquippedBySlot[chosenSlot] = instanceToEquip;
		return true;
	}

	/// <summary>Clears every equipment dictionary entry referencing the same instance as <paramref name="slot"/>.</summary>
	public static bool TryUnequipSlot(InventoryState inventory, EquipmentSlot slot)
	{
		if (!inventory.EquippedBySlot.TryGetValue(slot, out var cur) || cur == null)
			return false;

		ClearAllSlotsForInstance(inventory, cur.InstanceId);
		return true;
	}

	private static bool TryEquipOneFromBackpackRowToFootprint(InventoryState inventory, ItemInstance backpackRow, EquipmentDefinition eqDef)
	{
		var footprint = FootprintSlots(eqDef);
		if (footprint.Count == 0)
			return false;

		var rowIndex = inventory.Items.FindIndex(i => i.InstanceId == backpackRow.InstanceId);
		if (rowIndex < 0 || backpackRow.Quantity <= 0)
			return false;

		if (!TrySplitStackForEquip(inventory, rowIndex, out var instanceToEquip))
			return false;

		ClearConflictsForSlots(inventory, footprint);
		foreach (var s in footprint)
			inventory.EquippedBySlot[s] = instanceToEquip;

		return true;
	}

	private static bool HasMultiSlotFootprint(EquipmentDefinition eqDef) =>
		FootprintSlots(eqDef).Count > 0;

	/// <summary>Deduped footprint from <see cref="EquipmentDefinition.OccupiedSlots"/> (first-seen order).</summary>
	public static IReadOnlyList<EquipmentSlot> FootprintSlots(EquipmentDefinition eqDef)
	{
		if (eqDef.OccupiedSlots == null || eqDef.OccupiedSlots.Count == 0)
			return Array.Empty<EquipmentSlot>();

		var seen = new HashSet<EquipmentSlot>();
		var list = new List<EquipmentSlot>();
		foreach (var s in eqDef.OccupiedSlots)
		{
			if (seen.Add(s))
				list.Add(s);
		}

		return list;
	}

	private static List<EquipmentSlot> DistinctSlotsPreserveOrder(List<EquipmentSlot> slots)
	{
		var seen = new HashSet<EquipmentSlot>();
		var list = new List<EquipmentSlot>();
		foreach (var s in slots)
		{
			if (seen.Add(s))
				list.Add(s);
		}

		return list;
	}

	private static void ClearConflictsForSlots(InventoryState inventory, IReadOnlyList<EquipmentSlot> targets)
	{
		var ids = new HashSet<Guid>();
		foreach (var s in targets)
		{
			if (!inventory.EquippedBySlot.TryGetValue(s, out var cur) || cur == null)
				continue;
			ids.Add(cur.InstanceId);
		}

		foreach (var id in ids)
			ClearAllSlotsForInstance(inventory, id);
	}

	private static void ClearAllSlotsForInstance(InventoryState inventory, Guid instanceId)
	{
		foreach (var key in inventory.EquippedBySlot.Keys.ToList())
		{
			if (inventory.EquippedBySlot.TryGetValue(key, out var cur) && cur != null && cur.InstanceId == instanceId)
				inventory.EquippedBySlot[key] = null;
		}
	}

	private static bool TrySplitStackForEquip(InventoryState inventory, int rowIndex, out ItemInstance instanceToEquip)
	{
		var row = inventory.Items[rowIndex];
		if (row.Quantity <= 0)
		{
			instanceToEquip = null!;
			return false;
		}

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

		return true;
	}
}
