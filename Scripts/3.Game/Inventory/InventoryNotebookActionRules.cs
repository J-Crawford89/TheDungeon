#nullable enable

/// <summary>Pure rules for enabling inventory notebook action buttons (testable without Godot).</summary>
public static class InventoryNotebookActionRules
{
	public static bool ShouldEnableEquip(InventoryNotebookSelection selection, ItemInstance? item) =>
		selection.Kind == InventorySlotKind.Backpack && item?.Definition is EquipmentDefinition;

	public static bool ShouldEnableUnequip(InventoryNotebookSelection selection, ItemInstance? item) =>
		selection.Kind == InventorySlotKind.Equipment && item != null;

	public static bool ShouldEnableUse(InventoryNotebookSelection selection, ItemInstance? item) =>
		item?.Definition is PotionDefinition;

	public static bool ShouldEnableDrop(ItemInstance? item) => item != null && item.Definition.CanDrop;

	public static bool ShouldEnableMoveToBelt(InventoryNotebookSelection selection, ItemInstance? item) =>
		selection.Kind == InventorySlotKind.Backpack && item != null;

	public static bool ShouldEnableCompare() => false;
}
