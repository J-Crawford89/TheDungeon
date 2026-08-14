using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class FloorGeneratorTests
{
	private sealed class EmptyMonsters : IMonsterDefinitionRepository { public IReadOnlyList<MonsterDefinition> All => []; }
	private sealed class EmptyTraps : ITrapDefinitionRepository { public IReadOnlyList<TrapDefinition> All => []; }
	private sealed class EmptyTreasure : ITreasureDefinitionRepository { public IReadOnlyList<TreasureDefinition> All => []; }
	private sealed class EmptyNpc : INpcDefinitionRepository { public IReadOnlyList<NpcDefinition> All => []; }
	private sealed class EmptyLore : ILoreDefinitionRepository { public IReadOnlyList<LoreDefinition> All => []; }
	private sealed class EmptyItems : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	private static RoomFeaturePopulationParameters NoFeatures =>
		new()
		{
			MinFeaturesPerRoom = 0,
			MaxFeaturesPerRoom = 0,
			ContinueAfterFirstOptionalFeatureProbability = 0,
			FeatureTypeRules = [],
		};

	private static FloorGenerator Generator()
	{
		var chestLoot = new ChestLootGenerator(new EmptyItems(), ChestLootGenerationParameters.Default);
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore(), chestLoot);
		return new FloorGenerator(population);
	}

	[Fact]
	public void Generate_SameSeed_ProducesStableRoomSet()
	{
		var gen = Generator();
		var parameters = new FloorGenerationParameters
		{
			Seed = 42,
			MinRooms = 6,
			MaxRooms = 6,
			CurrentFloorCount = 0,
			RoomFeatures = NoFeatures,
		};

		var a = gen.Generate(parameters);
		var b = gen.Generate(parameters);

		Assert.Equal(a.Rooms.Count, b.Rooms.Count);
		Assert.Equal(a.Rooms.Keys.OrderBy(c => c.X).ThenBy(c => c.Y), b.Rooms.Keys.OrderBy(c => c.X).ThenBy(c => c.Y));
		Assert.Equal(a.Entrance, b.Entrance);
		Assert.Equal(a.Level, b.Level);
	}

	[Fact]
	public void Generate_RoomCount_StaysWithinMinMax()
	{
		var gen = Generator();
		var floor = gen.Generate(new FloorGenerationParameters
		{
			Seed = 7,
			MinRooms = 3,
			MaxRooms = 5,
			CurrentFloorCount = 0,
			RoomFeatures = NoFeatures,
		});

		Assert.InRange(floor.Rooms.Count, 3, 5);
	}

	[Fact]
	public void Generate_Entrance_IsOriginOnLevelOne()
	{
		var floor = Generator().Generate(new FloorGenerationParameters
		{
			Seed = 1,
			MinRooms = 4,
			MaxRooms = 4,
			CurrentFloorCount = 0,
			RoomFeatures = NoFeatures,
		});

		Assert.Equal(1, floor.Level);
		Assert.Equal(DirectionHelper.Origin, floor.Entrance);
		Assert.True(floor.Rooms.ContainsKey(floor.Entrance));
	}

	[Fact]
	public void Generate_PreviousFloorConnection_PlacedOnEntranceRoom()
	{
		var floor = Generator().Generate(new FloorGenerationParameters
		{
			Seed = 3,
			MinRooms = 4,
			MaxRooms = 4,
			CurrentFloorCount = 1,
			PreviousFloorConnectionType = FloorConnectionType.Stairs,
			RoomFeatures = NoFeatures,
		});

		Assert.Equal(2, floor.Level);
		Assert.True(DungeonFloorLayoutService.TryGetRoom(floor, floor.Entrance, out var entrance));
		var exit = RoomFeatureHelper.GetFeature<FloorExitFeature>(entrance!);
		Assert.NotNull(exit);
		Assert.Equal(FloorConnectionType.Stairs, exit!.ExitType);
	}
}
