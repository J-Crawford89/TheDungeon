using Xunit;

public sealed class InventoryStateAddStackTests
{
	[Fact]
	public void AddOrStack_FillsExistingThenNewRow()
	{
		var def = new ItemDefinition { Id = "bolt", Name = "Bolt", MaxStackSize = 10 };
		var inv = new InventoryState();
		inv.Items.Add(new ItemInstance { Definition = def, Quantity = 8 });

		var leftover = inv.AddOrStack(def, 2);

		Assert.Equal(0, leftover);
		Assert.Single(inv.Items);
		Assert.Equal(10, inv.Items[0].Quantity);

		var leftover2 = inv.AddOrStack(def, 5);
		Assert.Equal(0, leftover2);
		Assert.Equal(2, inv.Items.Count);
		Assert.Equal(5, inv.Items[1].Quantity);
	}

	[Fact]
	public void AddOrStack_LargeQuantity_SplitsAcrossStacks()
	{
		var def = new ItemDefinition { Id = "arrows", Name = "Arrows", MaxStackSize = 5 };
		var inv = new InventoryState();

		var leftover = inv.AddOrStack(def, 12);

		Assert.Equal(0, leftover);
		Assert.Equal(3, inv.Items.Count);
		Assert.Equal(5, inv.Items[0].Quantity);
		Assert.Equal(5, inv.Items[1].Quantity);
		Assert.Equal(2, inv.Items[2].Quantity);
	}
}
