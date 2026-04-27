using System.Collections.Generic;
using Xunit;

public sealed class HandBuiltDungeonFloorTests
{
	private sealed class EmptyMonsters : IMonsterDefinitionRepository { public IReadOnlyList<MonsterDefinition> All => []; }
	private sealed class EmptyTraps : ITrapDefinitionRepository { public IReadOnlyList<TrapDefinition> All => []; }
	private sealed class EmptyTreasure : ITreasureDefinitionRepository { public IReadOnlyList<TreasureDefinition> All => []; }
	private sealed class EmptyNpc : INpcDefinitionRepository { public IReadOnlyList<NpcDefinition> All => []; }
	private sealed class EmptyLore : ILoreDefinitionRepository { public IReadOnlyList<LoreDefinition> All => []; }

	private static RoomFeaturePopulationParameters NoRandomFeatures =>
		new()
		{
			MinFeaturesPerRoom = 0,
			MaxFeaturesPerRoom = 4,
			FeatureTypeRules = [],
		};

	[Fact]
	public void CreateSample_WithEmptyDefinitionRepos_ProducesLinkedFloorWithEntrance()
	{
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore());

		var floor = HandBuiltDungeonFloor.CreateSample(population, NoRandomFeatures);

		Assert.Equal(1, floor.Level);
		Assert.True(floor.Rooms.ContainsKey(floor.Entrance));
		Assert.NotEmpty(floor.Rooms);
		Assert.InRange(floor.Rooms.Count, 4, 20);
	}

	[Fact]
	public void CreateSample_AllRoomsReachableFromEntrance()
	{
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore());
		var floor = HandBuiltDungeonFloor.CreateSample(population, NoRandomFeatures);
		var start = floor.Entrance;
		var seen = new HashSet<RoomCoord> { start };
		var q = new Queue<RoomCoord>();
		q.Enqueue(start);
		while (q.Count > 0)
		{
			var c = q.Dequeue();
			if (!floor.Rooms.TryGetValue(c, out var room))
				continue;
			foreach (var exit in room.Exits.All())
			{
				if (!DungeonNavigationHelper.IsConnectionTraversable(exit.Connection))
					continue;
				var n = DungeonNavigationHelper.GetNeighborCoord(c, exit.Side);
				if (!floor.Rooms.ContainsKey(n))
					continue;
				if (seen.Add(n))
					q.Enqueue(n);
			}
		}

		Assert.Equal(floor.Rooms.Count, seen.Count);
	}
}
