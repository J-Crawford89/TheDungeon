#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Fills <see cref="ChestFeature"/> contents from category weights and the item definition repository.</summary>
public sealed class ChestLootGenerator
{
	private readonly IItemDefinitionRepository _items;
	private readonly ChestLootGenerationParameters _parameters;

	private readonly Dictionary<ChestLootCategory, List<ItemDefinition>> _pools;

	public ChestLootGenerator(IItemDefinitionRepository items, ChestLootGenerationParameters parameters)
	{
		_items = items;
		_parameters = parameters;
		_pools = BuildPools();
	}

	private Dictionary<ChestLootCategory, List<ItemDefinition>> BuildPools()
	{
		var map = new Dictionary<ChestLootCategory, List<ItemDefinition>>();
		foreach (ChestLootCategory c in Enum.GetValues<ChestLootCategory>())
			map[c] = [];

		foreach (var def in _items.All.Where(static d => d.CanDrop))
		{
			var cat = ChestLootCategoryMapping.FromItem(def);
			map[cat].Add(def);
		}

		return map;
	}

	public List<LootableItemDefinition> GenerateLootRows(Random random)
	{
		var rows = new List<LootableItemDefinition>();
		var minS = Math.Max(0, _parameters.MinStacks);
		var maxS = Math.Max(minS, _parameters.MaxStacks);
		var stackCount = minS == maxS ? minS : random.Next(minS, maxS + 1);

		var qMin = Math.Max(1, Math.Min(_parameters.QuantityMin, _parameters.QuantityMax));
		var qMax = Math.Max(qMin, _parameters.QuantityMax);

		for (var i = 0; i < stackCount; i++)
		{
			var weighted = BuildWeightedCategories();
			if (weighted.Count == 0)
				break;

			var cat = WeightedRandomSelection.Pick(random, weighted);
			var pool = _pools[cat];
			if (pool.Count == 0)
				continue;

			var item = pool[random.Next(pool.Count)];
			var qty = random.Next(qMin, qMax + 1);
			qty = Math.Clamp(qty, 1, Math.Max(1, item.MaxStackSize));

			rows.Add(new LootableItemDefinition { ItemDefinitionId = item.Id, Quantity = qty });
		}

		return rows;
	}

	private List<(ChestLootCategory cat, double weight)> BuildWeightedCategories()
	{
		var list = new List<(ChestLootCategory cat, double weight)>();
		foreach (var row in _parameters.CategoryWeights)
		{
			if (row.Weight <= 0)
				continue;
			if (!_pools.TryGetValue(row.Category, out var pool) || pool.Count == 0)
				continue;
			list.Add((row.Category, row.Weight));
		}

		return list;
	}
}
