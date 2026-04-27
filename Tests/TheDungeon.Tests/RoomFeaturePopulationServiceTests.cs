using System.Collections.Generic;
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

	[Fact]
	public void Populate_Level1Origin_IsSkippedEvenWhenMinimumWouldPopulate()
	{
		var service = new RoomFeaturePopulationService(new Monsters(), new Traps(), new Treasures(), new Npcs(), new LoreRepo());
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
}
