using System.Linq;
using Xunit;

public sealed class PlayerActionTargetResolversTests
{
	private static TreasureDefinition Gold(string id, string name) =>
		new()
		{
			Id = id,
			Name = name,
			GrantKind = TreasureKind.Gold,
			ValueInGp = 1,
		};

	private static GameSessionState SessionWithRoom(DungeonRoom room)
	{
		var s = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		s.Dungeon.CurrentFloor = floor;
		s.Dungeon.PlayerCoord = DirectionHelper.Origin;
		return s;
	}

	[Fact]
	public void ResolveTakeTargets_Exploration_MultipleItems_PrependsTakeAll_WithMatchingHighlightKeys()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance { IsRevealed = true, Definition = Gold("a", "Pile A") },
			],
		});
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance { IsRevealed = true, Definition = Gold("b", "Pile B") },
			],
		});
		var session = SessionWithRoom(room);

		var exploration = PlayerActionTargetResolvers.ResolveTakeTargets(session, DungeonMode.Exploration);
		Assert.Equal(3, exploration.Count);
		Assert.Equal("Take all", exploration[0].Label);
		Assert.Null(exploration[0].HighlightKey);
		Assert.Equal(TargetPayloadKind.TakeAllEligibleTreasure, exploration[0].Payload.Kind);
		var expectedKeys = MainViewRoomSlots.CollectTakeAllTreasureHighlightKeys(room);
		Assert.Equal(expectedKeys.Count, exploration[0].TakeAllHighlightKeys.Count);
		Assert.Equal(expectedKeys.OrderBy(x => x), exploration[0].TakeAllHighlightKeys.OrderBy(x => x));

		var combat = PlayerActionTargetResolvers.ResolveTakeTargets(session, DungeonMode.Combat);
		Assert.Equal(2, combat.Count);
		Assert.All(combat, d => Assert.Equal(TargetPayloadKind.TakeTreasureItem, d.Payload.Kind));
	}

	[Fact]
	public void ResolveTakeTargets_SingleItem_NoTakeAllAggregate()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance { IsRevealed = true, Definition = Gold("a", "Only pile") },
			],
		});
		var session = SessionWithRoom(room);
		var list = PlayerActionTargetResolvers.ResolveTakeTargets(session, DungeonMode.Exploration);
		Assert.Single(list);
		Assert.Equal(TargetPayloadKind.TakeTreasureItem, list[0].Payload.Kind);
	}

	[Fact]
	public void ResolveAttackTargets_CountsLivingMonstersInOrder()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 3,
					Definition = new MonsterDefinition { Id = "rat", Name = "Rat", Defense = 10 },
				},
				new MonsterInstance
				{
					CurrentHp = 0,
					Definition = new MonsterDefinition { Id = "rat", Name = "Dead", Defense = 10 },
				},
				new MonsterInstance
				{
					CurrentHp = 2,
					Definition = new MonsterDefinition { Id = "giant_rat", Name = "Big", Defense = 11 },
				},
			],
		});
		var session = SessionWithRoom(room);
		var list = PlayerActionTargetResolvers.ResolveAttackTargets(session);
		Assert.Equal(2, list.Count);
		Assert.Equal($"{MainViewRoomSlots.MonsterKeyPrefix}0", list[0].HighlightKey);
		Assert.Equal($"{MainViewRoomSlots.MonsterKeyPrefix}1", list[1].HighlightKey);
		Assert.Equal(0, list[0].Payload.LivingMonsterOrdinal);
		Assert.Equal(1, list[1].Payload.LivingMonsterOrdinal);
	}

	[Fact]
	public void ResolveDisarmTargets_OneRowPerRevealedTrap()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var def = new TrapDefinition { Id = "t1", Name = "Spikes", DisarmDc = 10, Damage = 1, Effect = "" };
		room.Features.Add(new TrapFeature
		{
			Traps =
			[
				new TrapInstance { Definition = def, IsRevealed = true, CurrentHp = 1 },
				new TrapInstance { Definition = def, IsRevealed = true, CurrentHp = 1 },
			],
		});
		var session = SessionWithRoom(room);
		var list = PlayerActionTargetResolvers.ResolveDisarmTargets(session);
		Assert.Equal(2, list.Count);
		Assert.Equal($"{MainViewRoomSlots.TrapKeyPrefix}0:0", list[0].HighlightKey);
		Assert.Equal($"{MainViewRoomSlots.TrapKeyPrefix}0:1", list[1].HighlightKey);
	}
}
