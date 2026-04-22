using Xunit;

public sealed class InventoryStateTests
{
	[Fact]
	public void SumQuantityForDefinitionId_AddsAcrossStacksAndQuantities()
	{
		var inv = new InventoryState();
		inv.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = InventoryIds.HealthPotion },
			Quantity = 2
		});
		inv.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = "other" },
			Quantity = 5
		});

		Assert.Equal(2, inv.SumQuantityForDefinitionId(InventoryIds.HealthPotion));
		Assert.Equal(0, inv.SumQuantityForDefinitionId(""));
	}

	[Fact]
	public void TryConsumeOne_DecrementsFirstStack_RemovesRowWhenEmpty()
	{
		var inv = new InventoryState();
		inv.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = "rope" },
			Quantity = 1
		});

		Assert.True(inv.TryConsumeOne("rope"));
		Assert.Empty(inv.Items);

		Assert.False(inv.TryConsumeOne("rope"));
	}

	[Fact]
	public void TryConsumeOne_ReducesQuantityWhenStackLargerThanOne()
	{
		var inv = new InventoryState();
		inv.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = "rope" },
			Quantity = 3
		});

		Assert.True(inv.TryConsumeOne("rope"));
		Assert.Single(inv.Items);
		Assert.Equal(2, inv.Items[0].Quantity);
	}

	[Fact]
	public void AddOrStackOne_StacksSameId_IncrementsQuantity()
	{
		var def = new ItemDefinition { Id = "rope", MaxStackSize = 99 };
		var inv = new InventoryState();
		inv.AddOrStackOne(def);
		inv.AddOrStackOne(def);
		Assert.Single(inv.Items);
		Assert.Equal(2, inv.Items[0].Quantity);
	}
}

