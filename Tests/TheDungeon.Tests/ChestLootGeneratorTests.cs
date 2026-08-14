using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class ChestLootGeneratorTests
{
	private sealed class MapItemRepo : IItemDefinitionRepository
	{
		private readonly Dictionary<string, ItemDefinition> _map = new();

		public MapItemRepo(params ItemDefinition[] defs)
		{
			foreach (var d in defs)
				_map[d.Id] = d;
		}

		public IReadOnlyList<ItemDefinition> All => _map.Values.ToList();
		public ItemDefinition? TryGetById(string id) =>
			string.IsNullOrWhiteSpace(id) ? null : (_map.TryGetValue(id.Trim(), out var d) ? d : null);
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	[Fact]
	public void GenerateLootRows_UsesOnlyWeightedCategoriesWithPoolItems()
	{
		var sword = new WeaponDefinition { Id = "short_sword", Name = "Short sword", MaxStackSize = 1, CanDrop = true };
		var repo = new MapItemRepo(sword);
		var gen = new ChestLootGenerator(repo, new ChestLootGenerationParameters
		{
			MinStacks = 2,
			MaxStacks = 2,
			QuantityMin = 1,
			QuantityMax = 1,
			CategoryWeights =
			[
				new ChestLootCategoryWeight { Category = ChestLootCategory.Weapon, Weight = 1 },
				new ChestLootCategoryWeight { Category = ChestLootCategory.Potion, Weight = 99 },
			],
		});

		var rows = gen.GenerateLootRows(new Random(1));

		Assert.Equal(2, rows.Count);
		Assert.All(rows, r => Assert.Equal("short_sword", r.ItemDefinitionId));
	}

	[Fact]
	public void GenerateLootRows_EmptyRepository_ReturnsNoRows()
	{
		var gen = new ChestLootGenerator(new MapItemRepo(), ChestLootGenerationParameters.Default);
		Assert.Empty(gen.GenerateLootRows(new Random(1)));
	}
}

public sealed class ChestLootCategoryMappingTests
{
	[Fact]
	public void FromItem_MapsDefinitionSubtypes()
	{
		Assert.Equal(ChestLootCategory.Weapon, ChestLootCategoryMapping.FromItem(new WeaponDefinition { Id = "w" }));
		Assert.Equal(ChestLootCategory.Armor, ChestLootCategoryMapping.FromItem(new ArmorDefinition { Id = "a" }));
		Assert.Equal(ChestLootCategory.Potion, ChestLootCategoryMapping.FromItem(new PotionDefinition { Id = "p" }));
		Assert.Equal(ChestLootCategory.ConsumableOther, ChestLootCategoryMapping.FromItem(new ConsumableDefinition { Id = "c" }));
		Assert.Equal(ChestLootCategory.Misc, ChestLootCategoryMapping.FromItem(new ItemDefinition { Id = "i" }));
	}
}
