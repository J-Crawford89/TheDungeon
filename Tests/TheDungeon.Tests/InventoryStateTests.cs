using Xunit;

public sealed class InventoryStateTests
{
	[Fact]
	public void SumQuantityForDefinitionId_AddsAcrossStacksAndQuantities()
	{
		var inv = new InventoryState();
		inv.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = InventoryIds.HealthPotionItemId },
			Quantity = 2
		});
		inv.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = "other" },
			Quantity = 5
		});

		Assert.Equal(2, inv.SumQuantityForDefinitionId(InventoryIds.HealthPotionItemId));
		Assert.Equal(0, inv.SumQuantityForDefinitionId(""));
	}
}
