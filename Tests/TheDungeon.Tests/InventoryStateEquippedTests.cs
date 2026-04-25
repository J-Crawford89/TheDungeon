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
		player.CharacterRaceId = "r";
		player.CharacterClassId = "c";
		player.CharacterBackgroundId = "b";
		player.TotalArmorBonus = 7;
		player.TotalDamageReduction = 4;
		player.InventoryState.Items.Add(new ItemInstance { Definition = new ItemDefinition { Id = "x" }, Quantity = 1 });
		player.InventoryState.EquippedBySlot[EquipmentSlot.Head] = player.InventoryState.Items[0];

		player.ResetToNewAdventurer();

		Assert.Empty(player.InventoryState.EquippedBySlot);
		Assert.Empty(player.InventoryState.Items);
		Assert.Equal("", player.CharacterRaceId);
		Assert.Equal("", player.CharacterClassId);
		Assert.Equal("", player.CharacterBackgroundId);
		Assert.Equal(0, player.TotalArmorBonus);
		Assert.Equal(0, player.TotalDamageReduction);
	}
}
