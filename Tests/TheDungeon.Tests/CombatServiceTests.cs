using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class CombatServiceTests
{
	private sealed class EmptyItemDefinitionRepository : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	private sealed class SinglePotionItemRepository : IItemDefinitionRepository
	{
		private readonly PotionDefinition _potion;

		public SinglePotionItemRepository()
		{
			_potion = new PotionDefinition
			{
				Id = InventoryIds.HealthPotion,
				Name = "Health Potion",
				MaxStackSize = 99,
				Effects =
				[
					new RestoreHealthEffectDefinition
					{
						FlatHealAmount = 1,
						HealDice = new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false },
					}
				],
			};
		}

		public IReadOnlyList<ItemDefinition> All => [_potion];

		public ItemDefinition? TryGetById(string id) =>
			string.Equals(id?.Trim(), _potion.Id, StringComparison.Ordinal) ? _potion : null;

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
			All.OfType<T>().ToArray();
	}

	private static CombatService CreateCombatService(Random? random = null)
	{
		var r = random ?? new Random(42);
		var dice = new DiceRollService(r);
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>());
		var items = new EmptyItemDefinitionRepository();
		var treasure = new TreasurePickupService(narrative, items, TestPlayerProficiencyAggregation.CreateEmpty());
		var potionFx = new PotionEffectApplicationService(dice, narrative, items);
		var traps = new TrapService(resolution, narrative, vitals, items);
		return new CombatService(dice, resolution, narrative, vitals, downed, treasure, potionFx, traps, items);
	}

	private static CombatService CreateCombatServiceWithPotionRepo(Random? random = null)
	{
		var r = random ?? new Random(42);
		var dice = new DiceRollService(r);
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>());
		var items = new SinglePotionItemRepository();
		var treasure = new TreasurePickupService(narrative, items, TestPlayerProficiencyAggregation.CreateEmpty());
		var potionFx = new PotionEffectApplicationService(dice, narrative, items);
		var traps = new TrapService(resolution, narrative, vitals, items);
		return new CombatService(dice, resolution, narrative, vitals, downed, treasure, potionFx, traps, items);
	}

	private static GameSessionState SessionWithWeaponCombat(WeaponDefinition? weapon, ItemInstance? weaponRow)
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 500,
					Definition = new MonsterDefinition
					{
						Id = "bag",
						Name = "Sandbag",
						Defense = -50,
						Attacks =
						[
							new AttackDefinition
							{
								Name = "Nudge",
								AttackModifier = 0,
								DamageComponents =
								[
									new DamageComponent(
										new DiceExpression { NumberOfDice = 0, DieType = DieType.d6, InD20CheckPool = false },
										0,
										new DamageTypeDefinition("monster.physical", "Physical", DamageFamily.Physical)),
								],
							},
						],
					},
				},
			],
		});
		var s = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		s.Dungeon.CurrentFloor = floor;
		s.Dungeon.PlayerCoord = DirectionHelper.Origin;
		s.Dungeon.DungeonMode = DungeonMode.Combat;
		if (weapon != null && weaponRow != null)
		{
			s.Player.InventoryState.Items.Add(weaponRow);
			s.Player.InventoryState.EquippedBySlot[EquipmentSlot.WeaponMainHand1] = weaponRow;
		}
		s.Combat = new CombatState
		{
			FleeReturnCoord = DirectionHelper.Origin,
			FleeReturnFloorLevel = 1,
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};
		return s;
	}

	private static int FirstMonsterHp(GameSessionState session) =>
		((MonsterFeature)session.Dungeon.CurrentRoom!.Features[0]).Monsters[0].CurrentHp;

	[Fact]
	public void CanAcceptPlayerAction_WhenPhaseGameOver_ReturnsFalse()
	{
		var combat = CreateCombatService();
		var session = new GameSessionState();
		session.Phase = GamePlayPhase.GameOver;
		Assert.False(combat.CanAcceptPlayerAction(session));
	}

	[Fact]
	public void CanAcceptPlayerAction_WhenNotInCombat_ReturnsFalse()
	{
		var combat = CreateCombatService();
		var session = new GameSessionState();
		Assert.False(combat.CanAcceptPlayerAction(session));
	}

	[Fact]
	public void CanExecuteCombatAbility_Defend_UsesRegistryWhenGrantedOnPlayerTurn()
	{
		var combat = CreateCombatService();
		var session = SessionWithWeaponCombat(null, null);
		session.Player.GrantedAbilities.Add(new GrantedAbility { AbilityId = AbilityIds.Defend });

		Assert.True(combat.CanAcceptPlayerAction(session));
		Assert.True(combat.CanExecuteCombatAbility(session, AbilityIds.Defend));
		Assert.False(combat.CanExecuteCombatAbility(session, "unknown"));
	}

	[Fact]
	public void CombatService_ImplementsICombatService()
	{
		var combat = CreateCombatService();
		Assert.IsAssignableFrom<ICombatService>(combat);
	}

	[Fact]
	public async Task ExecutePlayerAttack_WithEquippedWeapon_EventuallyDealsDamageForSomeSeed()
	{
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
					Name = "Swing",
					AttackModifier = 0,
					AbilityScore = AbilityScore.Might,
					AddAbilityScoreToDamage = true,
					DamageComponents =
					[
						new DamageComponent(
							new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
							0,
							pierce),
					],
				},
			],
		};
		var weaponRow = new ItemInstance { Definition = club, Quantity = 1 };

		var foundHit = false;
		for (var seed = 0; seed < 12_000; seed++)
		{
			var session = SessionWithWeaponCombat(club, weaponRow);
			var hpBefore = FirstMonsterHp(session);
			var combat = CreateCombatService(new Random(seed));
			await combat.ExecutePlayerAttackAsync(session, 0, PlayerAttackChoice.Weapon(EquipmentSlot.WeaponMainHand1));
			if (FirstMonsterHp(session) < hpBefore)
			{
				foundHit = true;
				break;
			}
		}

		Assert.True(foundHit);
	}

	[Fact]
	public async Task TryBeginCombatIfHostile_WhenNoLivingMonsters_ReturnsFalse()
	{
		var combat = CreateCombatService(new Random(1));
		var session = new GameSessionState();
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 0,
					Definition = new MonsterDefinition { Id = "m", Name = "Dead", Attacks = [] }
				},
			]
		});
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;

		Assert.False(await combat.TryBeginCombatIfHostileAsync(session, DirectionHelper.Origin, 1));
		Assert.Null(session.Combat);
	}

	[Fact]
	public async Task TryBeginCombatIfHostile_WhenHostile_StartsCombatState()
	{
		var combat = CreateCombatService(new Random(2));
		var session = new GameSessionState();
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 2,
					Definition = new MonsterDefinition
					{
						Id = "rat",
						Name = "Rat",
						Defense = 10,
						Attacks =
						[
							new AttackDefinition
							{
								Name = "Bite",
								DamageComponents =
								[
									new DamageComponent(new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false }, 0,
										new DamageTypeDefinition("p", "Physical", DamageFamily.Physical)),
								],
							},
						]
					},
				},
			]
		});
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;

		Assert.True(await combat.TryBeginCombatIfHostileAsync(session, DirectionHelper.Origin, 1));
		Assert.NotNull(session.Combat);
		Assert.Equal(DungeonMode.Combat, session.Dungeon.DungeonMode);
		Assert.NotEmpty(session.Combat!.TurnOrder);
	}

	[Fact]
	public async Task ExecutePlayerFlee_WhenSuccess_RestoresExploration()
	{
		var combat = CreateCombatService(new Random(3));
		var session = new GameSessionState();
		var floor1 = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var floor2 = new DungeonFloor { Level = 2, Entrance = DirectionHelper.Origin };
		floor2.Rooms[DirectionHelper.Origin] = new DungeonRoom { Position = DirectionHelper.Origin };
		session.Dungeon.Floors.Add(floor1);
		session.Dungeon.Floors.Add(floor2);
		session.Dungeon.CurrentFloor = floor2;
		session.Dungeon.PlayerCoord = new RoomCoord(5, 5);
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Player.AbilityScores.Agility = 0;
		session.Combat = new CombatState
		{
			FleeDc = -100,
			FleeReturnCoord = new RoomCoord(1, 2),
			FleeReturnFloorLevel = 1,
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};

		await combat.ExecutePlayerFleeAsync(session);

		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
		Assert.Equal(new RoomCoord(1, 2), session.Dungeon.PlayerCoord);
		Assert.Equal(1, session.Dungeon.CurrentFloor!.Level);
	}

	[Fact]
	public async Task ExecutePlayerFlee_WhenFail_StaysInCombat()
	{
		var combat = CreateCombatService(new Random(4));
		var session = new GameSessionState();
		session.Dungeon.CurrentFloor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState
		{
			FleeDc = 100,
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};

		await combat.ExecutePlayerFleeAsync(session);

		Assert.NotNull(session.Combat);
		Assert.Equal(DungeonMode.Combat, session.Dungeon.DungeonMode);
		Assert.Contains(session.LogEntries, l => l.Text.Contains("Flee", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExecutePlayerAttack_InvalidTarget_LogsAndReturns()
	{
		var weapon = new WeaponDefinition { Id = "w", Name = "W", Attacks = [] };
		var row = new ItemInstance { Definition = weapon, Quantity = 1 };
		var session = SessionWithWeaponCombat(weapon, row);
		var combat = CreateCombatService(new Random(5));

		await combat.ExecutePlayerAttackAsync(session, 99, PlayerAttackChoice.Weapon(EquipmentSlot.WeaponMainHand1));

		Assert.Contains(session.LogEntries, l => l.Text.Contains("nothing you can attack", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExecutePlayerAttack_MissingWeapon_FallsBackToUnarmed()
	{
		var session = SessionWithWeaponCombat(null, null);
		var hpBefore = FirstMonsterHp(session);
		var combat = CreateCombatService(new Random(6));

		await combat.ExecutePlayerAttackAsync(session, 0, PlayerAttackChoice.Weapon(EquipmentSlot.WeaponMainHand1));

		Assert.True(FirstMonsterHp(session) < hpBefore);
		Assert.Contains(session.LogEntries, l => l.Text.Contains("strike unarmed", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExecutePlayerAttack_WeaponWithNoAttacks_LogsAndDoesNotDamage()
	{
		var weapon = new WeaponDefinition
		{
			Id = "empty",
			Name = "Broken Sword",
			Slots = [EquipmentSlot.WeaponMainHand1],
			Attacks = [],
		};
		var row = new ItemInstance { Definition = weapon, Quantity = 1 };
		var session = SessionWithWeaponCombat(weapon, row);
		var hpBefore = FirstMonsterHp(session);
		var combat = CreateCombatService(new Random(7));

		await combat.ExecutePlayerAttackAsync(session, 0, PlayerAttackChoice.Weapon(EquipmentSlot.WeaponMainHand1));

		Assert.Equal(hpBefore, FirstMonsterHp(session));
		Assert.Contains(session.LogEntries, l => l.Text.Contains("no attacks configured", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExecutePlayerTakeTreasure_WrongPayloadKind_DoesNotAdvanceTurn()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Combat!.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		var combat = CreateCombatService(new Random(8));

		await combat.ExecutePlayerTakeTreasureAsync(session, new TargetPayload { Kind = TargetPayloadKind.AttackLivingMonsterOrdinal });

		Assert.Equal(0, session.Combat.CurrentTurnIndex);
	}

	[Fact]
	public async Task ExecutePlayerDisarmTrap_WrongPayloadKind_DoesNotAdvanceTurn()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Combat!.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		var combat = CreateCombatService(new Random(9));

		await combat.ExecutePlayerDisarmTrapAsync(session, new TargetPayload { Kind = TargetPayloadKind.AttackLivingMonsterOrdinal });

		Assert.Equal(0, session.Combat.CurrentTurnIndex);
	}

	[Fact]
	public async Task ExecutePlayerUseHealthPotion_NoneLeft_DoesNotAdvanceTurn()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Combat!.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		var combat = CreateCombatService(new Random(10));

		await combat.ExecutePlayerUseHealthPotionAsync(session);

		Assert.Equal(0, session.Combat.CurrentTurnIndex);
	}

	[Fact]
	public async Task ExecutePlayerUseHealthPotion_Applied_AdvancesTurn()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Combat!.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		session.Player.CurrentHp = 5;
		session.Player.MaxHp = 10;
		session.Player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = new PotionDefinition { Id = InventoryIds.HealthPotion, Name = "Potion", MaxStackSize = 99 },
			Quantity = 1
		});
		var combat = CreateCombatServiceWithPotionRepo(new Random(11));

		await combat.ExecutePlayerUseHealthPotionAsync(session);

		Assert.Equal(1, session.Combat.CurrentTurnIndex);
		Assert.True(session.Player.CurrentHp > 5);
	}

	[Fact]
	public async Task ExecutePlayerDefend_WithoutAbility_DoesNotAdvanceTurn()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Combat!.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		var combat = CreateCombatService(new Random(12));

		await combat.ExecutePlayerDefendAsync(session);

		Assert.Equal(0, session.Combat.CurrentTurnIndex);
	}

	[Fact]
	public async Task ExecutePlayerFlee_Success_WhenReturnFloorMissing_StillRestoresExplorationAndCoord()
	{
		var combat = CreateCombatService(new Random(13));
		var session = new GameSessionState();
		var floor2 = new DungeonFloor { Level = 2, Entrance = DirectionHelper.Origin };
		session.Dungeon.Floors.Add(floor2);
		session.Dungeon.CurrentFloor = floor2;
		session.Dungeon.PlayerCoord = new RoomCoord(9, 9);
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState
		{
			FleeDc = -100,
			FleeReturnCoord = new RoomCoord(1, 1),
			FleeReturnFloorLevel = 999,
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};

		await combat.ExecutePlayerFleeAsync(session);

		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
		Assert.Equal(new RoomCoord(1, 1), session.Dungeon.PlayerCoord);
		Assert.Equal(2, session.Dungeon.CurrentFloor!.Level);
	}

	[Fact]
	public async Task ExecutePlayerDefend_WhenAlreadyDefending_LogsMessageAndDoesNotAdvance()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Player.GrantedAbilities.Add(new GrantedAbility { AbilityId = AbilityIds.Defend });
		session.Combat!.ActiveCombatEffects.Add(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);
		session.Combat.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		var combat = CreateCombatService(new Random(14));

		await combat.ExecutePlayerDefendAsync(session);

		Assert.Equal(0, session.Combat.CurrentTurnIndex);
		Assert.Contains(session.LogEntries, l => l.Text.Contains("already defending", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExecutePlayerDefend_WhenOnCooldown_LogsMessageAndDoesNotAdvance()
	{
		var session = SessionWithWeaponCombat(null, null);
		session.Player.GrantedAbilities.Add(new GrantedAbility { AbilityId = AbilityIds.Defend });
		session.Combat!.AbilityCooldowns.Start(AbilityIds.Defend, 1);
		session.Combat.TurnOrder = [new CombatTurnSlot { IsPlayer = true }, new CombatTurnSlot { IsPlayer = true }];
		session.Combat.CurrentTurnIndex = 0;
		var combat = CreateCombatService(new Random(15));

		await combat.ExecutePlayerDefendAsync(session);

		Assert.Equal(0, session.Combat.CurrentTurnIndex);
		Assert.Contains(session.LogEntries, l => l.Text.Contains("cannot defend yet", StringComparison.OrdinalIgnoreCase));
	}
}
