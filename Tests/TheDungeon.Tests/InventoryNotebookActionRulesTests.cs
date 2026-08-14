using Xunit;

public sealed class InventoryNotebookActionRulesTests
{
	[Fact]
	public void ShouldEnableEquip_BackpackWithWeapon_True()
	{
		var sel = InventoryNotebookSelection.Backpack(0);
		var item = new ItemInstance
		{
			Definition = new WeaponDefinition { Id = "sword", Slots = [EquipmentSlot.WeaponMainHand1] },
			Quantity = 1,
		};
		Assert.True(InventoryNotebookActionRules.ShouldEnableEquip(sel, item));
	}

	[Fact]
	public void ShouldEnableEquip_BackpackWithPlainItem_False()
	{
		var sel = InventoryNotebookSelection.Backpack(0);
		var item = new ItemInstance { Definition = new ItemDefinition { Id = "rope" }, Quantity = 1 };
		Assert.False(InventoryNotebookActionRules.ShouldEnableEquip(sel, item));
	}

	[Fact]
	public void ShouldEnableUnequip_EquipmentWithItem_True()
	{
		var sel = InventoryNotebookSelection.Equipment(EquipmentSlot.Head);
		var item = new ItemInstance { Definition = new ItemDefinition { Id = "hat" }, Quantity = 1 };
		Assert.True(InventoryNotebookActionRules.ShouldEnableUnequip(sel, item));
	}

	[Fact]
	public void ShouldEnableUse_PotionDefinition_True()
	{
		var sel = InventoryNotebookSelection.Backpack(2);
		var item = new ItemInstance { Definition = new PotionDefinition { Id = "mana_potion" }, Quantity = 1 };
		Assert.True(InventoryNotebookActionRules.ShouldEnableUse(sel, item));
	}

	[Fact]
	public void ShouldEnableUse_NonPotion_False()
	{
		var sel = InventoryNotebookSelection.Backpack(2);
		var item = new ItemInstance { Definition = new ItemDefinition { Id = InventoryIds.HealthPotion }, Quantity = 1 };
		Assert.False(InventoryNotebookActionRules.ShouldEnableUse(sel, item));
	}

	[Fact]
	public void ShouldEnableCompare_AlwaysFalse()
	{
		Assert.False(InventoryNotebookActionRules.ShouldEnableCompare());
	}
}
