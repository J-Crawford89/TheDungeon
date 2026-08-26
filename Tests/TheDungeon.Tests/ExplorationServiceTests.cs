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
		public Task<bool> TryBeginCombatIfHostileAsync(GameSessionState session, RoomCoord previousCoord, int floorLevel) => Task.FromResult(false);
		public bool CanAcceptPlayerAction(GameSessionState session) => false;
		public bool IsCombatAbilityVisible(GameSessionState session, string abilityId) => false;
		public bool CanExecuteCombatAbility(GameSessionState session, string abilityId) => false;
		public Task ExecutePlayerAttackAsync(GameSessionState session, int livingMonsterOrdinal, PlayerAttackChoice attackChoice) => Task.CompletedTask;
		public Task ExecutePlayerFleeAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerTakeTreasureAsync(GameSessionState session, TargetPayload payload) => Task.CompletedTask;
		public Task ExecutePlayerUseHealthPotionAsync(GameSessionState session) => Task.CompletedTask;
		public Task ExecutePlayerCombatAbilityAsync(GameSessionState session, string abilityId) => Task.CompletedTask;
		public Task ExecutePlayerDisarmTrapAsync(GameSessionState session, TargetPayload payload) => Task.CompletedTask;
	}

	private static ExplorationService Service()
	{
		var chestLoot = new ChestLootGenerator(new EmptyItems(), ChestLootGenerationParameters.Default);
		var population = new RoomFeaturePopulationService(
			new EmptyMonsters(), new EmptyTraps(), new EmptyTreasure(), new EmptyNpc(), new EmptyLore(), chestLoot);
		var dice = new DiceRollService(new System.Random(1));
		var inspect = new InspectService(dice, new ResolutionService(dice), new NarrativeService());
		var trapService = new TrapService(new ResolutionService(dice), new NarrativeService(), new PlayerVitalsService(), new EmptyItems());
		return new ExplorationService(new FloorGenerator(population), new NoopCombatService(), inspect, trapService);
	}

	[Fact]
	public async Task Inspect_CorpseFeature_DoesNotEmitRemains_KeepsSlainFromDeadMonster()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new CorpseFeature
		{
			Contents = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 }],
		});
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 0,
					Definition = new MonsterDefinition
					{
						Id = "rat",
						Name = "Rat",
						Defense = 1,
						Attacks =
						[
							new AttackDefinition
							{
								Name = "Bite",
								DamageComponents =
								[
									new DamageComponent(
										new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
										0,
										new DamageTypeDefinition("p", "Physical", DamageFamily.Physical)),
								],
							},
						],
					},
				},
			],
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = await service.InspectAsync(session);

		Assert.True(result.Success);
		Assert.DoesNotContain(result.InspectData!.FeatureLines, l => l.Text.StartsWith("Remains:"));
		Assert.Contains(result.InspectData.FeatureLines, l => l.Text == "Slain: Rat (carcass)");
	}

	[Fact]
	public async Task Inspect_IncludesSalvageFeatureLine()
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

		var result = await service.InspectAsync(session);

		Assert.True(result.Success);
		var line = Assert.Single(result.InspectData!.FeatureLines, l => l.Text.StartsWith("Salvage:"));
		Assert.Contains("1 stack", line.Text);
	}

	[Fact]
	public async Task Inspect_IncludesChestFeatureLine()
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

		var result = await service.InspectAsync(session);

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
	public async Task Inspect_NoRoomAtPlayer_ReturnsInspectNoRoom()
	{
		var service = Service();
		var session = new GameSessionState();
		session.Dungeon.CurrentFloor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = await service.InspectAsync(session);

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

	[Fact]
	public void MoveForward_LinkedNeighbor_MovesPlayerToDestination()
	{
		var service = Service();
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var origin = new DungeonRoom { Position = DirectionHelper.Origin };
		var northCoord = DungeonNavigationHelper.GetNeighborCoord(DirectionHelper.Origin, HorizontalDirection.North);
		var north = new DungeonRoom { Position = northCoord };
		floor.Rooms[origin.Position] = origin;
		floor.Rooms[north.Position] = north;
		Assert.True(DungeonFloorLayoutService.TryLinkRooms(
			floor,
			origin.Position,
			HorizontalDirection.North,
			RoomConnectionType.Passage,
			out _));
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Player.Facing = HorizontalDirection.North;
		session.Dungeon.DiscoveredRoomsByFloor[1] = [DirectionHelper.Origin];

		var result = service.MoveForward(session);

		Assert.True(result.Success);
		Assert.Equal(northCoord, session.Dungeon.PlayerCoord);
		Assert.Equal(northCoord, result.DestinationAfterMove);
	}

	[Fact]
	public void MoveDownAFloor_UnanchoredHoleWithoutRope_DoesNotTripTrapsOrGenerateFloor()
	{
		var service = Service();
		var session = new GameSessionState();
		session.Player.CurrentHp = 10;
		session.Player.MaxHp = 10;
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new FloorExitFeature { ExitType = FloorConnectionType.Hole });
		room.Features.Add(new TrapFeature
		{
			Traps =
			[
				new TrapInstance
				{
					CurrentHp = 1,
					IsRevealed = true,
					Definition = new TrapDefinition
					{
						Id = "snare",
						Name = "Snare",
						Damage = 3,
						IsRemovedAfterTripped = true,
					},
				},
			],
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.Floors.Add(floor);
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var result = service.MoveDownAFloor(session, new FloorGenerationParameters
		{
			Seed = 1,
			MinRooms = 3,
			MaxRooms = 3,
			CurrentFloorCount = 1,
			PreviousFloorConnectionType = FloorConnectionType.Hole,
			RoomFeatures = new RoomFeaturePopulationParameters
			{
				MinFeaturesPerRoom = 0,
				MaxFeaturesPerRoom = 0,
				ContinueAfterFirstOptionalFeatureProbability = 0,
				FeatureTypeRules = [],
			},
		});

		Assert.False(result.Success);
		Assert.Equal(ExplorationErrorCode.MoveBlocked, result.ErrorCode);
		Assert.Single(session.Dungeon.Floors);
		Assert.Equal(10, session.Player.CurrentHp);
		Assert.True(RoomFeatureHelper.HasFeature<TrapFeature>(room));
	}

	[Fact]
	public void MoveDownAFloor_UnanchoredHoleWithRope_ConsumesRopeBeforeTrapsAndGenerate()
	{
		var service = Service();
		var session = new GameSessionState();
		session.Player.CurrentHp = 10;
		session.Player.MaxHp = 10;
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new ItemDefinition { Id = InventoryIds.Rope, Name = "Rope", MaxStackSize = 99 },
			Quantity = 1,
		});
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var hole = new FloorExitFeature { ExitType = FloorConnectionType.Hole };
		room.Features.Add(hole);
		room.Features.Add(new TrapFeature
		{
			Traps =
			[
				new TrapInstance
				{
					CurrentHp = 1,
					IsRevealed = true,
					Definition = new TrapDefinition
					{
						Id = "snare",
						Name = "Snare",
						Damage = 3,
						IsRemovedAfterTripped = true,
					},
				},
			],
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.Floors.Add(floor);
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DiscoveredRoomsByFloor[1] = [DirectionHelper.Origin];

		var result = service.MoveDownAFloor(session, new FloorGenerationParameters
		{
			Seed = 11,
			MinRooms = 3,
			MaxRooms = 3,
			DoorChance = 0,
			CurrentFloorCount = 1,
			PreviousFloorConnectionType = FloorConnectionType.Hole,
			RoomFeatures = new RoomFeaturePopulationParameters
			{
				MinFeaturesPerRoom = 0,
				MaxFeaturesPerRoom = 0,
				ContinueAfterFirstOptionalFeatureProbability = 0,
				FeatureTypeRules = [],
			},
		});

		Assert.True(result.Success);
		Assert.True(result.ConsumedRopeForHole);
		Assert.Equal(0, session.Player.InventoryState.SumQuantityForDefinitionId(InventoryIds.Rope));
		Assert.True(hole.RopeAnchored);
		Assert.Equal(2, session.Dungeon.Floors.Count);
		Assert.Equal(7, session.Player.CurrentHp);
		Assert.False(RoomFeatureHelper.HasFeature<TrapFeature>(room));
	}
}
