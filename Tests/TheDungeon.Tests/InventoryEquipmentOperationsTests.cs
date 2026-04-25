using Xunit;

public sealed class InventoryEquipmentOperationsTests
{
	private static ItemInstance Stack(string id, int qty) =>
		new()
		{
			Definition = new ItemDefinition { Id = id, Name = id, MaxStackSize = 99 },
			Quantity = qty,
		};

	[Fact]
	public void TryEquipOneFromBackpackRow_EquipsSingleStackRow()
	{
		var helm = new EquipmentDefinition
		{
			Id = "helm",
			Name = "Helm",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.Head],
		};
		var row = new ItemInstance { Definition = helm, Quantity = 1 };
		var inv = new InventoryState { Items = { row } };

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, row));
		Assert.Single(inv.Items);
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.Head, out var e) && ReferenceEquals(e, row));
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_StackSplit_CreatesNewInstance()
	{
		var helm = new EquipmentDefinition
		{
			Id = "helm",
			Name = "Helm",
			MaxStackSize = 99,
			Slots = [EquipmentSlot.Head],
		};
		var row = new ItemInstance { Definition = helm, Quantity = 3 };
		var inv = new InventoryState { Items = { row } };

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, row));
		Assert.Equal(2, row.Quantity);
		Assert.Equal(2, inv.Items.Count);
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.Head, out var equipped) && equipped != null);
		Assert.NotSame(row, equipped);
		Assert.Equal(1, equipped!.Quantity);
		Assert.Equal(helm.Id, equipped.Definition.Id);
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_Swap_ReplacesEquipped()
	{
		var oldHelm = new EquipmentDefinition
		{
			Id = "old",
			Name = "Old",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.Head],
		};
		var newHelm = new EquipmentDefinition
		{
			Id = "new",
			Name = "New",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.Head],
		};
		var worn = new ItemInstance { Definition = oldHelm, Quantity = 1 };
		var backpack = new ItemInstance { Definition = newHelm, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { worn, backpack },
			EquippedBySlot = { [EquipmentSlot.Head] = worn },
		};

		Assert.True(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, backpack));
		Assert.True(inv.EquippedBySlot.TryGetValue(EquipmentSlot.Head, out var e) && ReferenceEquals(e, backpack));
		Assert.Contains(worn, inv.Items);
		Assert.Contains(backpack, inv.Items);
	}

	[Fact]
	public void TryUnequipSlot_ClearsSlot()
	{
		var helm = new EquipmentDefinition { Id = "h", Name = "H", Slots = [EquipmentSlot.Head] };
		var row = new ItemInstance { Definition = helm, Quantity = 1 };
		var inv = new InventoryState
		{
			Items = { row },
			EquippedBySlot = { [EquipmentSlot.Head] = row },
		};

		Assert.True(InventoryEquipmentOperations.TryUnequipSlot(inv, EquipmentSlot.Head));
		Assert.Null(inv.EquippedBySlot[EquipmentSlot.Head]);
		Assert.Contains(row, inv.Items);
	}

	[Fact]
	public void TryEquipOneFromBackpackRow_NonEquipment_ReturnsFalse()
	{
		var inv = new InventoryState { Items = { Stack("junk", 1) } };
		Assert.False(InventoryEquipmentOperations.TryEquipOneFromBackpackRow(inv, inv.Items[0]));
	}
}
