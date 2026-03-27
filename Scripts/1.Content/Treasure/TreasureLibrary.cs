using System.Collections.Generic;

public static class TreasureLibrary
{
	public static readonly TreasureDefinition CopperCoins = new()
	{
		Id = "copper_coins",
		Name = "A pouch of copper coins",
		GrantKind = TreasureKind.Gold,
		ValueInGp = 5
	};

	public static readonly TreasureDefinition HealthPotion = new()
	{
		Id = "health_potion",
		Name = "A health potion",
		GrantKind = TreasureKind.InventoryItem,
		InventoryItemId = InventoryConstants.HealthPotionItemId
	};

	public static IReadOnlyList<TreasureDefinition> All => new[] { CopperCoins, HealthPotion };
}
