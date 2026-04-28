using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class RoomFeaturePopulationServiceTests
{
	private sealed class Monsters : IMonsterDefinitionRepository
	{
		public IReadOnlyList<MonsterDefinition> All =>
		[
			new MonsterDefinition
			{
				Id = "m",
				Name = "Rat",
				MaxHp = 2,
				RandomizerWeight = 1,
				Attacks = [],
			}
		];
	}

	private sealed class Traps : ITrapDefinitionRepository
	{
		public IReadOnlyList<TrapDefinition> All => [];
	}

	private sealed class Treasures : ITreasureDefinitionRepository
	{
		public IReadOnlyList<TreasureDefinition> All => [];
	}

	private sealed class Npcs : INpcDefinitionRepository
	{
		public IReadOnlyList<NpcDefinition> All => [];
	}

	private sealed class LoreRepo : ILoreDefinitionRepository
	{
		public IReadOnlyList<LoreDefinition> All => [];
	}

	private sealed class ItemsRepo : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All =>
		[
			new WeaponDefinition
			{
				Id = "blade",
				Name = "Blade",
				MaxStackSize = 1,
				Attacks = [],
				Category = new WeaponCategoryDefinition(),
				SubCategory = new WeaponSubCategoryDefinition(),
				Group = new WeaponGroupDefinition(),
				SubGroup = new WeaponSubGroupDefinition(),
			},
		];

		public ItemDefinition? TryGetById(string id) =>
			string.Equals(id, "blade", System.StringComparison.Ordinal) ? All[0] : null;

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	[Fact]
	public void Populate_Level1Origin_IsSkippedEvenWhenMinimumWouldPopulate()
	{
		var chestLoot = new ChestLootGenerator(new ItemsRepo(), ChestLootGenerationParameters.Default);
		var service = new RoomFeaturePopulationService(new Monsters(), new Traps(), new Treasures(), new Npcs(), new LoreRepo(), chestLoot);
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var origin = new DungeonRoom { Position = DirectionHelper.Origin };
		var otherCoord = new RoomCoord(1, 0);
		var other = new DungeonRoom { Position = otherCoord };
		floor.Rooms[DirectionHelper.Origin] = origin;
		floor.Rooms[otherCoord] = other;
		var parameters = new RoomFeaturePopulationParameters
		{
			MinFeaturesPerRoom = 1,
			MaxFeaturesPerRoom = 1,
			ContinueAfterFirstOptionalFeatureProbability = 0,
			FeatureTypeRules =
			[
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Monster,
					Weight = 1,
					MinPerRoom = 0,
					MaxPerRoom = 1,
					AllowedInEntrance = true,
					AllowedInExit = true
				}
			]
		};

		service.Populate(floor, parameters, new System.Random(1));

		Assert.Empty(origin.Features);
		Assert.Single(other.Features);
		Assert.IsType<MonsterFeature>(other.Features[0]);
	}

	[Fact]
	public void Populate_ChestRule_AddsChestWithLootFromRepo()
	{
		var lootParams = new ChestLootGenerationParameters
		{
			MinStacks = 1,
			MaxStacks = 1,
			QuantityMin = 1,
			QuantityMax = 1,
			CategoryWeights =
			[
				new ChestLootCategoryWeight { Category = ChestLootCategory.Weapon, Weight = 1 },
			],
		};
		var chestLoot = new ChestLootGenerator(new ItemsRepo(), lootParams);
		var service = new RoomFeaturePopulationService(new Monsters(), new Traps(), new Treasures(), new Npcs(), new LoreRepo(), chestLoot);
		var floor = new DungeonFloor { Level = 2, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = new DungeonRoom { Position = DirectionHelper.Origin };
		var parameters = new RoomFeaturePopulationParameters
		{
			MinFeaturesPerRoom = 1,
			MaxFeaturesPerRoom = 1,
			ContinueAfterFirstOptionalFeatureProbability = 0,
			FeatureTypeRules =
			[
				new FeatureTypeRule
				{
					Kind = PopulateableFeatureKind.Chest,
					Weight = 1,
					MinPerRoom = 1,
					MaxPerRoom = 1,
					AllowedInEntrance = true,
					AllowedInExit = true,
				},
			],
		};

		service.Populate(floor, parameters, new System.Random(42));

		var room = floor.Rooms[DirectionHelper.Origin];
		var chest = Assert.Single(room.Features.OfType<ChestFeature>());
		Assert.NotEmpty(chest.Contents);
		Assert.Equal("blade", chest.Contents[0].ItemDefinitionId);
		Assert.True(chest.Contents[0].Quantity >= 1);
	}
}
