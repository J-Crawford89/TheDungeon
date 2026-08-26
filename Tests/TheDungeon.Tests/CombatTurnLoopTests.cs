using System;
using System.Collections.Generic;
using Xunit;

/// <summary>Friend assembly tests for internal <c>CombatTurnLoop</c> (InternalsVisibleTo from TheDungeon.Game).</summary>
public sealed class CombatTurnLoopTests
{
	private static readonly DamageTypeDefinition TestPhysical =
		new("test.physical", "Physical", DamageFamily.Physical);

	private sealed class CombatReadinessMirror : ICombatTurnReadiness
	{
		public bool IsPlayerTurn(GameSessionState session)
		{
			if (session.Phase != GamePlayPhase.InProgress)
				return false;
			if (session.Combat is not { } c || session.Dungeon.DungeonMode != DungeonMode.Combat)
				return false;
			if (c.TurnOrder.Count == 0)
				return false;
			var slot = c.TurnOrder[c.CurrentTurnIndex];
			return slot.IsPlayer;
		}
	}

	private sealed class SequentialDiceRoll : IDiceRollRequestExecutor
	{
		private readonly Queue<DiceRollResult> _queue;

		public SequentialDiceRoll(params DiceRollResult[] results) =>
			_queue = new Queue<DiceRollResult>(results);

		public DiceRollResult Roll(DiceRollRequest request)
		{
			if (_queue.Count == 0)
				throw new InvalidOperationException("SequentialDiceRoll: no more scripted rolls.");
			return _queue.Dequeue();
		}

		public DieRollResult RollDie(DieType dieType) =>
			new() { DieType = dieType, RolledValue = 1 };
	}

	private static DiceRollResult ScriptRoll(int total, int? resolvedD20) =>
		new()
		{
			SummaryText = "fixed",
			DetailText = "",
			RollTotal = total,
			ModifierTotal = 0,
			Total = total,
			ResolvedD20CheckValue = resolvedD20,
			ExpressionResults = [],
			ModifiersWithSources = [],
		};

	private static CombatMonsterTurn CreateMonsterTurn(IDiceRollRequestExecutor dice)
	{
		var resolution = new ResolutionService(dice);
		return new CombatMonsterTurn(
			resolution,
			dice,
			new NarrativeService(),
			new PlayerVitalsService(),
			new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>()));
	}

	private static CombatTurnLoop CreateLoop(out NarrativeService narrative) =>
		CreateLoop(new SequentialDiceRoll(), out narrative);

	private static CombatTurnLoop CreateLoop(IDiceRollRequestExecutor dice, out NarrativeService narrative)
	{
		narrative = new NarrativeService();
		var lifecycle = new CombatEncounterLifecycle(narrative);
		var monsterTurn = CreateMonsterTurn(dice);
		return new CombatTurnLoop(new CombatReadinessMirror(), lifecycle, monsterTurn);
	}

	private static GameSessionState SessionInCombat(List<CombatTurnSlot> order, int currentTurnIndex)
	{
		var session = new GameSessionState();
		session.Phase = GamePlayPhase.InProgress;
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState
		{
			FleeReturnCoord = DirectionHelper.Origin,
			FleeReturnFloorLevel = 1,
			TurnOrder = order,
			CurrentTurnIndex = currentTurnIndex,
		};
		return session;
	}

	private static MonsterFeature FeatureWithLivingMonster(int hp = 10)
	{
		var def = new MonsterDefinition
		{
			Id = "test",
			Name = "Tester",
			MaxHp = hp,
			Defense = 10,
			AbilityScores = new AbilityScores(),
			Attacks = [],
		};
		return new MonsterFeature
		{
			Monsters = [new MonsterInstance { CurrentHp = hp, Definition = def }],
		};
	}

	[Fact]
	public async Task ProcessAutomaticMonsterTurns_CurrentRoomNull_EndsCombatVictory()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 }],
			currentTurnIndex: 0);
		session.Dungeon.CurrentFloor = null;

		await loop.ProcessAutomaticMonsterTurnsAsync(session);

		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
	}

	[Fact]
	public async Task ProcessAutomaticMonsterTurns_NoMonsterFeature_EndsCombatVictory()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 }],
			currentTurnIndex: 0);
		var floor = new DungeonFloor { Level = 1 };
		var coord = DirectionHelper.Origin;
		floor.Rooms[coord] = new DungeonRoom { Position = coord };
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = coord;

		await loop.ProcessAutomaticMonsterTurnsAsync(session);

		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
	}

	[Fact]
	public async Task ProcessAutomaticMonsterTurns_AfterOneMonsterTurn_ResumesOnPlayerAndStillInCombat()
	{
		var attack = new AttackDefinition
		{
			Name = "Claw",
			AbilityScore = AbilityScore.Might,
			AddAbilityScoreToDamage = false,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
					0,
					TestPhysical),
			],
		};
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 10,
					Definition = new MonsterDefinition
					{
						Id = "loop_m",
						Name = "Test Beast",
						MaxHp = 10,
						Defense = 10,
						AbilityScores = new AbilityScores(),
						Attacks = [attack],
					},
				},
			],
		};
		var dice = new SequentialDiceRoll(
			ScriptRoll(18, 18),
			ScriptRoll(3, null));
		var loop = CreateLoop(dice, out _);
		var session = SessionInCombat(
			[
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 },
			],
			currentTurnIndex: 0);
		session.Player.CurrentHp = 100;
		session.Player.MaxHp = 100;
		var floor = new DungeonFloor { Level = 1 };
		var coord = DirectionHelper.Origin;
		var room = new DungeonRoom { Position = coord };
		room.Features.Add(feature);
		floor.Rooms[coord] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = coord;

		await loop.ProcessAutomaticMonsterTurnsAsync(session);

		Assert.NotNull(session.Combat);
		Assert.Equal(DungeonMode.Combat, session.Dungeon.DungeonMode);
		Assert.Equal(1, session.Combat!.CurrentTurnIndex);
		Assert.True(session.Combat.TurnOrder[1].IsPlayer);
		Assert.True(session.Player.CurrentHp < 100);
		Assert.Contains(session.LogEntries, e => e.Text.Contains("hits you", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ProcessAutomaticMonsterTurns_PlayerSlot_DecrementsAbilityCooldownsOnce()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 }],
			currentTurnIndex: 0);
		session.Combat!.AbilityCooldowns.Start("defend", 2);

		await loop.ProcessAutomaticMonsterTurnsAsync(session);

		Assert.Equal(1, session.Combat!.AbilityCooldowns.GetRemaining("defend"));
	}

	[Fact]
	public void NotifyPlayerTurnStarted_ReducesCooldownWhenAwaitingPlayer()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 }],
			currentTurnIndex: 0);
		session.Combat!.AbilityCooldowns.Start("bash", 3);

		loop.NotifyPlayerTurnStarted(session);

		Assert.Equal(2, session.Combat!.AbilityCooldowns.GetRemaining("bash"));
	}

	[Fact]
	public void NotifyPlayerTurnStarted_NoOpWhenCurrentSlotIsMonster()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 }],
			currentTurnIndex: 0);
		session.Combat!.AbilityCooldowns.Start("bash", 3);

		loop.NotifyPlayerTurnStarted(session);

		Assert.Equal(3, session.Combat!.AbilityCooldowns.GetRemaining("bash"));
	}

	[Fact]
	public void PruneDeadMonstersFromTurnOrder_WhenCurrentDeadSlotRemoved_IndexFallsBackWithMin()
	{
		var loop = CreateLoop(out _);
		var deadDef = new MonsterDefinition
		{
			Id = "m0",
			Name = "Dead",
			MaxHp = 5,
			Defense = 10,
			AbilityScores = new AbilityScores(),
			Attacks = [],
		};
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance { CurrentHp = 0, Definition = deadDef },
			],
		};
		var session = SessionInCombat(
			[
				new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
			],
			currentTurnIndex: 1);

		loop.PruneDeadMonstersFromTurnOrder(session, feature);

		Assert.Single(session.Combat!.TurnOrder);
		Assert.True(session.Combat.TurnOrder[0].IsPlayer);
		Assert.Equal(0, session.Combat.CurrentTurnIndex);
	}

	[Fact]
	public void PruneDeadMonstersFromTurnOrder_WhenCurrentMonsterStillAlive_ReindexesSameTurn()
	{
		var loop = CreateLoop(out _);
		var feature = FeatureWithLivingMonster();
		var session = SessionInCombat(
			[
				new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
			],
			currentTurnIndex: 1);

		loop.PruneDeadMonstersFromTurnOrder(session, feature);

		Assert.Equal(2, session.Combat!.TurnOrder.Count);
		Assert.Equal(1, session.Combat.CurrentTurnIndex);
		Assert.False(session.Combat.TurnOrder[1].IsPlayer);
		Assert.Equal(0, session.Combat.TurnOrder[1].MonsterIndex);
	}

	[Fact]
	public void AdvanceTurn_WrapsCurrentIndexModuloOrderCount()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[
				new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 1 },
			],
			currentTurnIndex: 2);

		loop.AdvanceTurn(session);

		Assert.Equal(0, session.Combat!.CurrentTurnIndex);
	}

	[Fact]
	public void CheckVictory_AllMonstersDead_EndsCombatVictory()
	{
		var loop = CreateLoop(out _);
		var session = SessionInCombat(
			[new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 }],
			currentTurnIndex: 0);
		var deadDef = new MonsterDefinition
		{
			Id = "gone",
			Name = "Corpse",
			MaxHp = 10,
			Defense = 10,
			AbilityScores = new AbilityScores(),
			Attacks = [],
		};
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance { CurrentHp = 0, Definition = deadDef },
			],
		};

		var ended = loop.CheckVictory(session, feature);

		Assert.True(ended);
		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
	}
}
