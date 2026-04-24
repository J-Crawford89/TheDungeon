using System;
using Xunit;

public sealed class InventoryNotebookSelectionTests
{
	[Fact]
	public void ResolveItem_None_ReturnsNull()
	{
		var inv = new InventoryState();
		var rows = Array.Empty<ItemInstance>();
		Assert.Null(InventoryNotebookSelection.None.ResolveItem(inv, rows));
	}

	[Fact]
	public void ResolveItem_Equipment_ReturnsEquipped()
	{
		var eq = new ItemInstance { Definition = new ItemDefinition { Id = "helm" }, Quantity = 1 };
		var inv = new InventoryState
		{
			EquippedBySlot = new System.Collections.Generic.Dictionary<EquipmentSlot, ItemInstance?>
			{
				[EquipmentSlot.Head] = eq,
			},
		};
		var sel = InventoryNotebookSelection.Equipment(EquipmentSlot.Head);
		Assert.Same(eq, sel.ResolveItem(inv, Array.Empty<ItemInstance>()));
	}

	[Fact]
	public void ResolveItem_Backpack_ReturnsRow()
	{
		var row = new ItemInstance { Definition = new ItemDefinition { Id = "a" }, Quantity = 1 };
		var inv = new InventoryState();
		var sel = InventoryNotebookSelection.Backpack(0);
		Assert.Same(row, sel.ResolveItem(inv, new[] { row }));
	}
}
