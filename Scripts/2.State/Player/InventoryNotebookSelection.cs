#nullable enable
using System;
using System.Collections.Generic;

/// <summary>Which UI slot is selected in the inventory notebook.</summary>
public readonly struct InventoryNotebookSelection : IEquatable<InventoryNotebookSelection>
{
	public InventorySlotKind Kind { get; private init; }
	public EquipmentSlot EquipmentSlot { get; private init; }
	public int BackpackGridIndex { get; private init; }

	public static InventoryNotebookSelection None => default;

	public static InventoryNotebookSelection Equipment(EquipmentSlot slot) =>
		new()
		{
			Kind = InventorySlotKind.Equipment,
			EquipmentSlot = slot,
		};

	public static InventoryNotebookSelection Backpack(int gridIndex) =>
		new()
		{
			Kind = InventorySlotKind.Backpack,
			BackpackGridIndex = gridIndex,
		};

	public ItemInstance? ResolveItem(InventoryState inventory, IReadOnlyList<ItemInstance> backpackRows)
	{
		return Kind switch
		{
			InventorySlotKind.None => null,
			InventorySlotKind.Equipment when inventory.EquippedBySlot.TryGetValue(EquipmentSlot, out var eq) => eq,
			InventorySlotKind.Backpack when BackpackGridIndex >= 0 && BackpackGridIndex < backpackRows.Count => backpackRows[BackpackGridIndex],
			_ => null,
		};
	}

	public bool Equals(InventoryNotebookSelection other) =>
		Kind == other.Kind && EquipmentSlot == other.EquipmentSlot && BackpackGridIndex == other.BackpackGridIndex;

	public override bool Equals(object? obj) => obj is InventoryNotebookSelection other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(Kind, EquipmentSlot, BackpackGridIndex);

	public static bool operator ==(InventoryNotebookSelection a, InventoryNotebookSelection b) => a.Equals(b);

	public static bool operator !=(InventoryNotebookSelection a, InventoryNotebookSelection b) => !a.Equals(b);
}
