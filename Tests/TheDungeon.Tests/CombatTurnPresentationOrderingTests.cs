#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class CombatTurnPresentationOrderingTests
{
	private static readonly DamageTypeDefinition TestPhysical =
		new("test.physical", "Physical", DamageFamily.Physical);

	private sealed class EmptyItems : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	private sealed class DeferredTurnSink : ICombatTurnPresentationSink
	{
		private readonly object _gate = new();
		private readonly List<TaskCompletionSource> _releases = [];
		private readonly List<CombatTurnPresentationKind> _kinds = [];

		public IReadOnlyList<CombatTurnPresentationKind> Kinds
		{
			get
			{
				lock (_gate)
					return _kinds.ToArray();
			}
		}

		public async Task WaitForCallCountAsync(int expected)
		{
			for (var attempt = 0; attempt < 1_000; attempt++)
			{
				lock (_gate)
				{
					if (_releases.Count >= expected)
						return;
				}

				await Task.Delay(1);
			}

			throw new TimeoutException($"Expected {expected} combat-turn presentation call(s).");
		}

		public void Complete(int zeroBasedCallIndex)
		{
			TaskCompletionSource release;
			lock (_gate)
				release = _releases[zeroBasedCallIndex];
			release.TrySetResult();
		}

		public Task PresentAsync(CombatTurnPresentationKind kind, CancellationToken ct = default)
		{
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			if (ct.CanBeCanceled)
				ct.Register(() => release.TrySetCanceled(ct));
			lock (_gate)
			{
				_kinds.Add(kind);
				_releases.Add(release);
				if (_releases.Count > 1)
					release.TrySetResult();
			}

			return release.Task;
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

	[Fact]
	public async Task TryBeginCombatIfHostileAsync_PresentsOrderRevealedBeforeAutomaticTurnsProceed()
	{
		var sink = new DeferredTurnSink();
		var host = new CombatTurnPresentationHost { Sink = sink };
		var combat = CreateCombatService(host);
		var (session, _) = BuildHostileRoom();
		var hpBefore = session.Player.CurrentHp;

		var action = combat.TryBeginCombatIfHostileAsync(session, DirectionHelper.Origin, 1);
		await sink.WaitForCallCountAsync(1);

		Assert.False(action.IsCompleted);
		Assert.NotNull(session.Combat);
		Assert.NotEmpty(session.Combat!.TurnOrder);
		Assert.Equal(CombatTurnPresentationKind.OrderRevealed, sink.Kinds[0]);
		Assert.Equal(hpBefore, session.Player.CurrentHp);
		Assert.DoesNotContain(session.LogEntries, e => e.Text.Contains("hits you", StringComparison.OrdinalIgnoreCase));

		sink.Complete(0);
		await action;

		Assert.True(action.IsCompleted);
	}

	[Fact]
	public async Task ProcessAutomaticMonsterTurnsAsync_PresentsActiveTurnChangedBeforePlayerTurnStarts()
	{
		var sink = new DeferredTurnSink();
		var host = new CombatTurnPresentationHost { Sink = sink };
		var dice = new SequentialDiceRoll(ScriptRoll(18, 18), ScriptRoll(3, null));
		var loop = CreateLoop(dice, host);
		var session = SessionInCombat(
			[
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 },
			],
			currentTurnIndex: 0);
		session.Player.CurrentHp = 100;
		session.Player.MaxHp = 100;
		session.Combat!.AbilityCooldowns.Start(AbilityIds.Defend, 2);
		PlaceMonster(session, CreateAttackingMonster());

		var action = loop.ProcessAutomaticMonsterTurnsAsync(session);
		await sink.WaitForCallCountAsync(1);

		Assert.False(action.IsCompleted);
		Assert.True(session.Player.CurrentHp < 100);
		Assert.Equal(CombatTurnPresentationKind.ActiveTurnChanged, sink.Kinds[0]);
		Assert.Equal(1, session.Combat.CurrentTurnIndex);
		Assert.Equal(2, session.Combat.AbilityCooldowns.GetRemaining(AbilityIds.Defend));

		sink.Complete(0);
		await action;

		Assert.True(session.Combat.TurnOrder[session.Combat.CurrentTurnIndex].IsPlayer);
		Assert.Equal(1, session.Combat.AbilityCooldowns.GetRemaining(AbilityIds.Defend));
	}

	[Fact]
	public async Task ExecutePlayerAttackAsync_PresentsActiveTurnChangedBeforeNextCombatantActs()
	{
		var sink = new DeferredTurnSink();
		var host = new CombatTurnPresentationHost { Sink = sink };
		var combat = CreateCombatService(host);
		var (session, monster) = BuildCombatWithPlayerFirst();
		var hpBefore = session.Player.CurrentHp;

		var action = combat.ExecutePlayerAttackAsync(session, 0, PlayerAttackChoice.Unarmed);
		await sink.WaitForCallCountAsync(1);

		Assert.False(action.IsCompleted);
		Assert.True(monster.CurrentHp < 500);
		Assert.Equal(CombatTurnPresentationKind.ActiveTurnChanged, sink.Kinds[0]);
		Assert.Equal(1, session.Combat!.CurrentTurnIndex);
		Assert.Equal(hpBefore, session.Player.CurrentHp);
		Assert.DoesNotContain(session.LogEntries, e => e.Text.Contains("hits you", StringComparison.OrdinalIgnoreCase));

		sink.Complete(0);
		await action;
	}

	[Fact]
	public async Task ExecutePlayerAttackAsync_WhenLastMonsterDies_PresentsCombatEndedAndWaits()
	{
		var sink = new DeferredTurnSink();
		var host = new CombatTurnPresentationHost { Sink = sink };
		var combat = CreateCombatService(host);
		var (session, monster) = BuildCombatWithPlayerFirst();
		monster.CurrentHp = 1;

		var action = combat.ExecutePlayerAttackAsync(session, 0, PlayerAttackChoice.Unarmed);
		await sink.WaitForCallCountAsync(1);

		Assert.False(action.IsCompleted);
		Assert.True(monster.CurrentHp <= 0);
		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
		Assert.Equal(CombatTurnPresentationKind.CombatEnded, sink.Kinds[0]);

		sink.Complete(0);
		await action;
	}

	[Fact]
	public async Task ExecutePlayerFleeAsync_WhenSuccess_PresentsCombatEndedAndWaits()
	{
		var sink = new DeferredTurnSink();
		var host = new CombatTurnPresentationHost { Sink = sink };
		var combat = CreateCombatService(host);
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

		var action = combat.ExecutePlayerFleeAsync(session);
		await sink.WaitForCallCountAsync(1);

		Assert.False(action.IsCompleted);
		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
		Assert.Equal(CombatTurnPresentationKind.CombatEnded, sink.Kinds[0]);

		sink.Complete(0);
		await action;
	}

	[Fact]
	public async Task ProcessAutomaticMonsterTurnsAsync_WhenPlayerDies_PresentsCombatEndedAndWaits()
	{
		var sink = new DeferredTurnSink();
		var host = new CombatTurnPresentationHost { Sink = sink };
		var dice = new SequentialDiceRoll(ScriptRoll(18, 18), ScriptRoll(3, null));
		var loop = CreateLoop(dice, host, new GameOverDownedHandler(new NarrativeService()));
		var session = SessionInCombat(
			[
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
				new CombatTurnSlot { IsPlayer = true, MonsterIndex = 0 },
			],
			currentTurnIndex: 0);
		session.Player.CurrentHp = 1;
		session.Player.MaxHp = 100;
		PlaceMonster(session, CreateAttackingMonster());

		var action = loop.ProcessAutomaticMonsterTurnsAsync(session);
		await sink.WaitForCallCountAsync(1);

		Assert.False(action.IsCompleted);
		Assert.Equal(GamePlayPhase.GameOver, session.Phase);
		Assert.Null(session.Combat);
		Assert.Equal(CombatTurnPresentationKind.CombatEnded, sink.Kinds[0]);

		sink.Complete(0);
		await action;
	}

	private static CombatService CreateCombatService(CombatTurnPresentationHost host)
	{
		var dice = new DiceRollService(new Random(42));
		var resolution = new ResolutionService(dice);
		var narrative = new NarrativeService();
		var vitals = new PlayerVitalsService();
		var downed = new PlayerDownedResolutionService(Array.Empty<IPlayerDownedOutcomeHandler>());
		var items = new EmptyItems();
		var treasure = new TreasurePickupService(narrative, items, TestPlayerProficiencyAggregation.CreateEmpty());
		var potionFx = new PotionEffectApplicationService(dice, narrative, items);
		var traps = new TrapService(resolution, narrative, vitals, items);
		return new CombatService(
			dice,
			resolution,
			narrative,
			vitals,
			downed,
			treasure,
			potionFx,
			traps,
			items,
			turnPresentationHost: host);
	}

	private static CombatTurnLoop CreateLoop(
		IDiceRollRequestExecutor dice,
		CombatTurnPresentationHost host,
		IPlayerDownedOutcomeHandler? downedHandler = null)
	{
		var narrative = new NarrativeService();
		var lifecycle = new CombatEncounterLifecycle(narrative);
		var resolution = new ResolutionService(dice);
		IPlayerDownedOutcomeHandler[] handlers = downedHandler == null
			? []
			: [downedHandler];
		var monsterTurn = new CombatMonsterTurn(
			resolution,
			dice,
			narrative,
			new PlayerVitalsService(),
			new PlayerDownedResolutionService(handlers));
		return new CombatTurnLoop(new CombatReadinessMirror(), lifecycle, monsterTurn, host);
	}

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
			return c.TurnOrder[c.CurrentTurnIndex].IsPlayer;
		}
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

	private static MonsterInstance CreateAttackingMonster(int hp = 10) =>
		new()
		{
			CurrentHp = hp,
			Definition = new MonsterDefinition
			{
				Id = "loop_m",
				Name = "Test Beast",
				MaxHp = hp,
				Defense = 10,
				AbilityScores = new AbilityScores(),
				Attacks =
				[
					new AttackDefinition
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
					},
				],
			},
		};

	private static void PlaceMonster(GameSessionState session, MonsterInstance monster)
	{
		var floor = new DungeonFloor { Level = 1 };
		var coord = DirectionHelper.Origin;
		var room = new DungeonRoom { Position = coord };
		room.Features.Add(new MonsterFeature { Monsters = [monster] });
		floor.Rooms[coord] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = coord;
	}

	private static (GameSessionState Session, MonsterInstance Monster) BuildHostileRoom()
	{
		var monster = CreateAttackingMonster(hp: 20);
		var session = new GameSessionState();
		session.Player.CurrentHp = 100;
		session.Player.MaxHp = 100;
		PlaceMonster(session, monster);
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		return (session, monster);
	}

	private static (GameSessionState Session, MonsterInstance Monster) BuildCombatWithPlayerFirst()
	{
		var monster = new MonsterInstance
		{
			CurrentHp = 500,
			Definition = new MonsterDefinition
			{
				Id = "ordering_target",
				Name = "Ordering Target",
				MaxHp = 500,
				Defense = -100,
				Attacks =
				[
					new AttackDefinition
					{
						Name = "Bite",
						DamageComponents =
						[
							new DamageComponent(
								new DiceExpression { NumberOfDice = 0, DieType = DieType.d4, InD20CheckPool = false },
								1,
								TestPhysical),
						],
					},
				],
			},
		};
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature { Monsters = [monster] });
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;

		var session = new GameSessionState();
		session.Player.CurrentHp = 100;
		session.Player.MaxHp = 100;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState
		{
			TurnOrder =
			[
				new CombatTurnSlot { IsPlayer = true },
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
			],
			CurrentTurnIndex = 0,
		};
		return (session, monster);
	}
}
