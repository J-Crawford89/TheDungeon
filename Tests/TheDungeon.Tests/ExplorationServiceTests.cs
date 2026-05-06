using System.Collections.Generic;
using Xunit;

public sealed class ExplorationServiceTests
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
	private sealed class NoopCombatService : ICombatService
	{
		public bool TryBeginCombatIfHostile(GameSessionState session, RoomCoord previousCoord, int floorLevel) => false;
		public bool IsAwaitingPlayerAction(GameSessionState session) => false;
		public void ExecutePlayerAttack(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) { }
		public void ExecutePlayerFlee(GameSessionState session) { }
		public void ExecutePlayerTakeTreasure(GameSessionState session, TargetPayload payload) { }
		public void ExecutePlayerUseHealthPotion(GameSessionState session) { }
		public void ExecutePlayerDefend(GameSessionState session) { }
		public void ExecutePlayerDisarmTrap(GameSessionState session, TargetPayload payload) { }
	}

	private static ExplorationService Service()
	{
		var chestLoot = new ChestLootGenerator(new EmptyItems(), ChestLootGenerationParameters.Default);
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore(), chestLoot);
		var dice = new DiceRollService(new System.Random(1));
		var inspect = new InspectService(dice, new ResolutionService(dice), new NarrativeService());
		var trapService = new TrapService(new ResolutionService(dice), new NarrativeService(), new PlayerVitalsService(), new EmptyItems());
		return new ExplorationService(population, new NoopCombatService(), inspect, trapService);
	}

	[Fact]
	public void Inspect_IncludesCorpseFeatureLine()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new CorpseFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 }],
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.Inspect(session);

		Assert.True(result.Success);
		var line = Assert.Single(result.InspectData!.FeatureLines, l => l.Text.StartsWith("Remains:"));
		Assert.Contains("1 stack", line.Text);
		Assert.DoesNotContain("harvest DC", line.Text, System.StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Inspect_IncludesSalvageFeatureLine()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new SalvageFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "rope", Quantity = 2 }],
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.Inspect(session);

		Assert.True(result.Success);
		var line = Assert.Single(result.InspectData!.FeatureLines, l => l.Text.StartsWith("Salvage:"));
		Assert.Contains("1 stack", line.Text);
	}

	[Fact]
	public void Inspect_IncludesChestFeatureLine()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new ChestFeature
		{
			Locked = true,
			Contents = [],
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.Inspect(session);

		Assert.True(result.Success);
		var line = Assert.Single(result.InspectData!.FeatureLines, l => l.Text.StartsWith("Chest:"));
		Assert.Contains("nothing inside", line.Text);
		Assert.Contains("(locked)", line.Text);
	}

	[Fact]
	public void MoveForward_NoCurrentFloor_FailsWithCode()
	{
		var service = Service();
		var session = new GameSessionState();

		var result = service.MoveForward(session);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.NoCurrentFloor, result.ErrorCode);
	}

	[Fact]
	public void MoveForward_BlockedExit_ReturnsMoveBlocked()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = new DungeonRoom { Position = DirectionHelper.Origin };
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Player.Facing = HorizontalDirection.North;

		var result = service.MoveForward(session);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.MoveBlocked, result.ErrorCode);
	}

	[Fact]
	public void MoveForward_TraversableExitButMissingNeighbor_ReturnsNoRoomAtTargetCoord()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Exits.Set(HorizontalDirection.North, RoomConnectionType.Passage);
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Player.Facing = HorizontalDirection.North;

		var result = service.MoveForward(session);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.NoRoomAtTargetCoord, result.ErrorCode);
	}

	[Fact]
	public void Turn_InvalidEnumValue_ReturnsTurnInvalid()
	{
		var service = Service();
		var player = new PlayerState();

		var result = service.Turn(player, (DirectionTurned)12345);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.TurnInvalid, result.ErrorCode);
	}

	[Fact]
	public void Inspect_NoRoomAtPlayer_ReturnsInspectNoRoom()
	{
		var service = Service();
		var session = new GameSessionState();
		session.Dungeon.CurrentFloor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.Inspect(session);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.InspectNoRoom, result.ErrorCode);
	}

	[Fact]
	public void MoveUpAFloor_NoPreviousFloor_ReturnsNoPreviousFloor()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new FloorExitFeature { ExitType = FloorConnectionType.Stairs });
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.Floors.Add(floor);
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.MoveUpAFloor(session);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.NoPreviousFloor, result.ErrorCode);
	}

	[Fact]
	public void MoveDownAFloor_NoVerticalConnection_ReturnsNoVerticalConnection()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = new DungeonRoom { Position = DirectionHelper.Origin };
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.MoveDownAFloor(session);

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.NoVerticalConnection, result.ErrorCode);
	}
}
