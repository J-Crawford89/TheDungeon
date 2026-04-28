/// <summary>Maps domain <see cref="ItemDefinition"/> instances to <see cref="ChestLootCategory"/> for loot tables.</summary>
public static class ChestLootCategoryMapping
{
	public static ChestLootCategory FromItem(ItemDefinition item) =>
		item switch
		{
			WeaponDefinition => ChestLootCategory.Weapon,
			ArmorDefinition => ChestLootCategory.Armor,
			PotionDefinition => ChestLootCategory.Potion,
			ConsumableDefinition => ChestLootCategory.ConsumableOther,
			_ => ChestLootCategory.Misc,
		};
}
