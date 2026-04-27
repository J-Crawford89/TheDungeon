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
		return new CombatService(dice, resolution, narrative, vitals, downed, treasure, potionFx, traps);
	}

	private static GameSessionState SessionWithWeaponCombat(WeaponDefinition weapon, ItemInstance weaponRow)
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
		s.Player.InventoryState.Items.Add(weaponRow);
		s.Player.InventoryState.EquippedBySlot[EquipmentSlot.WeaponMainHand1] = weaponRow;
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
	public void IsAwaitingPlayerAction_WhenPhaseGameOver_ReturnsFalse()
	{
		var combat = CreateCombatService();
		var session = new GameSessionState();
		session.Phase = GamePlayPhase.GameOver;
		Assert.False(combat.IsAwaitingPlayerAction(session));
	}

	[Fact]
	public void IsAwaitingPlayerAction_WhenNotInCombat_ReturnsFalse()
	{
		var combat = CreateCombatService();
		var session = new GameSessionState();
		Assert.False(combat.IsAwaitingPlayerAction(session));
	}

	[Fact]
	public void CombatService_ImplementsICombatService()
	{
		var combat = CreateCombatService();
		Assert.IsAssignableFrom<ICombatService>(combat);
	}

	[Fact]
	public void ExecutePlayerAttack_WithEquippedWeapon_EventuallyDealsDamageForSomeSeed()
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
			combat.ExecutePlayerAttack(session, 0, PlayerAttackChoice.Weapon(EquipmentSlot.WeaponMainHand1));
			if (FirstMonsterHp(session) < hpBefore)
			{
				foundHit = true;
				break;
			}
		}

		Assert.True(foundHit);
	}
}
