using Xunit;

public sealed class InventoryStateEquippedTests
{
	[Fact]
	public void NewInventoryState_HasEmptyEquippedBySlot()
	{
		var inv = new InventoryState();
		Assert.NotNull(inv.EquippedBySlot);
		Assert.Empty(inv.EquippedBySlot);
	}

	[Fact]
	public void ResetToNewAdventurer_ClearsEquippedWithNewInventory()
	{
		var player = new PlayerState();
		player.InventoryState.Items.Add(new ItemInstance { Definition = new ItemDefinition { Id = "x" }, Quantity = 1 });
		player.InventoryState.EquippedBySlot[EquipmentSlot.Head] = player.InventoryState.Items[0];

		player.ResetToNewAdventurer();

		Assert.Empty(player.InventoryState.EquippedBySlot);
		Assert.Empty(player.InventoryState.Items);
	}
}
