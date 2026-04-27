using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class CombatCorpseHelperTests
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
	public void Spawn_AlwaysAddsCorpse_EvenWhenDeathLootEmpty()
	{
		var session = new GameSessionState();
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var def = new MonsterDefinition { Id = "m", Name = "M", DeathLoot = [], HarvestDc = 5 };
		var repo = new MapItemRepo();

		CombatCorpseHelper.SpawnCorpseOnMonsterDeath(session, room, def, repo, new NarrativeService());

		var corpse = Assert.Single(room.Features.OfType<CorpseFeature>());
		Assert.Empty(corpse.Contents);
		Assert.Equal(5, corpse.HarvestDc);
	}

	[Fact]
	public void Spawn_AddsValidatedLootRows()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var session = new GameSessionState();
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var def = new MonsterDefinition
		{
			Id = "rat",
			Name = "Rat",
			DeathLoot = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 3 }],
		};

		CombatCorpseHelper.SpawnCorpseOnMonsterDeath(session, room, def, repo, new NarrativeService());

		var corpse = Assert.Single(room.Features.OfType<CorpseFeature>());
		var row = Assert.Single(corpse.Contents);
		Assert.Equal("coin", row.ItemDefinitionId);
		Assert.Equal(3, row.Quantity);
	}

	[Fact]
	public void Spawn_UnknownItemId_OmitsRowAndLogs()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var session = new GameSessionState();
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		const string badId = "missing_xyz";
		var def = new MonsterDefinition
		{
			Id = "rat",
			Name = "Rat",
			DeathLoot =
			[
				new LootableItemDefinition { ItemDefinitionId = badId, Quantity = 1 },
				new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 2 },
			],
		};

		CombatCorpseHelper.SpawnCorpseOnMonsterDeath(session, room, def, repo, new NarrativeService());

		var corpse = Assert.Single(room.Features.OfType<CorpseFeature>());
		var row = Assert.Single(corpse.Contents);
		Assert.Equal("coin", row.ItemDefinitionId);
		Assert.Contains(session.LogEntries,
			e => e.Text.Contains($"[Loot] Item definition '{badId}' not found.", System.StringComparison.Ordinal));
	}

	[Fact]
	public void ExecutePlayerAttack_OnKill_SpawnsCorpseWithLoot()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var items = new MapItemRepo(coin);
		var dice = new DiceRollService(new System.Random(42));
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService([]);
		var treasure = new TreasurePickupService(narrative, items, TestPlayerProficiencyAggregation.CreateEmpty());
		var potionFx = new PotionEffectApplicationService(dice, narrative, items);
		var traps = new TrapService(resolution, narrative, vitals, items);
		var containerLoot = new ContainerLootInteractionService(items, narrative, TestPlayerProficiencyAggregation.CreateEmpty());
		var combat = new CombatService(dice, resolution, narrative, vitals, downed, treasure, potionFx, traps, containerLoot, items);

		var pierce = new DamageTypeDefinition("p", "Piercing", DamageFamily.Physical);
		var club = new WeaponDefinition
		{
			Id = "club",
			Name = "Club",
			MaxStackSize = 1,
			Slots = [EquipmentSlot.WeaponMainHand1],
			Attacks =
			[
				new AttackDefinition
				{
					Name = "Bonk",
					AbilityScore = AbilityScore.Might,
					AddAbilityScoreToDamage = true,
					DamageComponents =
					[
						new DamageComponent(
							new DiceExpression { NumberOfDice = 99, DieType = DieType.d6, InD20CheckPool = false },
							0,
							pierce),
					],
				},
			],
		};
		var weaponRow = new ItemInstance { Definition = club, Quantity = 1 };

		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 1,
					Definition = new MonsterDefinition
					{
						Id = "rat",
						Name = "Rat",
						Defense = -100,
						ExperienceReward = 1,
						DeathLoot = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 1 }],
						Attacks =
						[
							new AttackDefinition
							{
								Name = "Nudge",
								DamageComponents =
								[
									new DamageComponent(
										new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false },
										0,
										pierce),
								],
							},
						],
					},
				},
			],
		});
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Player.InventoryState.Items.Add(weaponRow);
		session.Player.InventoryState.EquippedBySlot[EquipmentSlot.WeaponMainHand1] = weaponRow;
		session.Combat = new CombatState
		{
			FleeReturnCoord = DirectionHelper.Origin,
			FleeReturnFloorLevel = 1,
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};

		combat.ExecutePlayerAttack(session, 0, PlayerAttackChoice.Weapon(EquipmentSlot.WeaponMainHand1));

		var corpse = Assert.Single(room.Features.OfType<CorpseFeature>());
		Assert.Single(corpse.Contents);
		Assert.Equal("coin", corpse.Contents[0].ItemDefinitionId);
	}

	[Fact]
	public void ContainerLoot_TryLootAll_AfterCombatCorpse_TransfersCoin()
	{
		var coin = new ItemDefinition { Id = "coin", Name = "Coin", MaxStackSize = 99 };
		var repo = new MapItemRepo(coin);
		var session = new GameSessionState();
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var def = new MonsterDefinition
		{
			Id = "x",
			Name = "X",
			DeathLoot = [new LootableItemDefinition { ItemDefinitionId = "coin", Quantity = 2 }],
		};
		CombatCorpseHelper.SpawnCorpseOnMonsterDeath(session, room, def, repo, new NarrativeService());
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var svc = new ContainerLootInteractionService(repo, new NarrativeService(), TestPlayerProficiencyAggregation.CreateEmpty());
		var result = svc.TryLootAll(session, 0);

		Assert.Equal(ContainerLootErrorCode.None, result.ErrorCode);
		Assert.Equal(1, result.StacksGranted);
		Assert.Equal(2, session.Player.InventoryState.SumQuantityForDefinitionId("coin"));
	}
}
