using System;
using System.Collections.Generic;

/// <summary>Rules for filling a <see cref="ChestFeature"/> when spawned procedurally (separate from room feature mix).</summary>
public sealed class ChestLootGenerationParameters
{
	public int MinStacks { get; init; }
	public int MaxStacks { get; init; }

	public int QuantityMin { get; init; }
	public int QuantityMax { get; init; }

	public IReadOnlyList<ChestLootCategoryWeight> CategoryWeights { get; init; } =
		Array.Empty<ChestLootCategoryWeight>();

	public static ChestLootGenerationParameters Default { get; } =
		new()
		{
			MinStacks = 1,
			MaxStacks = 3,
			QuantityMin = 1,
			QuantityMax = 3,
			CategoryWeights =
			[
				new ChestLootCategoryWeight { Category = ChestLootCategory.Weapon, Weight = 2 },
				new ChestLootCategoryWeight { Category = ChestLootCategory.Armor, Weight = 2 },
				new ChestLootCategoryWeight { Category = ChestLootCategory.Potion, Weight = 4 },
				new ChestLootCategoryWeight { Category = ChestLootCategory.ConsumableOther, Weight = 1 },
				new ChestLootCategoryWeight { Category = ChestLootCategory.Misc, Weight = 1 },
			],
		};
}
