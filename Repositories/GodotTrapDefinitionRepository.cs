#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotTrapDefinitionRepository : ITrapDefinitionRepository
{
	private readonly IReadOnlyList<TrapDefinition> _all;

	public GodotTrapDefinitionRepository(TrapResourceDatabase? database)
	{
		if (database?.Traps != null && database.Traps.Count > 0)
			_all = database.Traps.Select(TrapMapper.ToDomain).ToList();
		else
			_all = DefaultAll();
	}

	public IReadOnlyList<TrapDefinition> All => _all;

	private static IReadOnlyList<TrapDefinition> DefaultAll() =>
		new[]
		{
			new TrapDefinition
			{
				Id = TrapIds.Snare,
				Name = "Snare trap",
				DiscoverDc = 10,
				DisarmDc = 12,
				Damage = 2,
				Effect = "Cord tightens around the ankle.",
				IsRemovedAfterTripped = true,
				DisarmLoot =
				[
					new LootableItemDefinition { ItemDefinitionId = InventoryIds.Rope, Quantity = 1 }
				],
			},
			new TrapDefinition
			{
				Id = "rusty_needle",
				Name = "Rusty needle trap",
				DiscoverDc = 12,
				DisarmDc = 14,
				Damage = 3,
				Effect = "A spring-loaded needle jabs out.",
				IsRemovedAfterTripped = true,
			}
		};
}
