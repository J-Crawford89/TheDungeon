#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotTreasureDefinitionRepository : ITreasureDefinitionRepository
{
	private readonly IReadOnlyList<TreasureDefinition> _all;

	public GodotTreasureDefinitionRepository(TreasureResourceDatabase? database)
	{
		if (database?.Treasures != null && database.Treasures.Count > 0)
			_all = database.Treasures.Select(TreasureMapper.ToDomain).ToList();
		else
			_all = DefaultAll();
	}

	public IReadOnlyList<TreasureDefinition> All => _all;

	private static IReadOnlyList<TreasureDefinition> DefaultAll() =>
		new[]
		{
			new TreasureDefinition
			{
				Id = TreasureIds.CopperCoins,
				Name = "A pouch of copper coins",
				GrantKind = TreasureKind.Gold,
				ValueInGp = 5
			},
			new TreasureDefinition
			{
				Id = TreasureIds.HealthPotion,
				Name = "A health potion",
				GrantKind = TreasureKind.InventoryItem,
				InventoryItemId = InventoryIds.HealthPotionItemId
			}
		};
}
