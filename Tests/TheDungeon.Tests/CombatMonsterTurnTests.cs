using System;
using System.Collections.Generic;
using Xunit;

public sealed class CombatMonsterTurnTests
{
	private static readonly DamageTypeDefinition Physical =
		new("test.physical", "Physical", DamageFamily.Physical);

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

	private static DiceRollResult Roll(int total, int? resolvedD20) =>
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

	private static MonsterFeature LivingMonsterFixture(int hp = 10)
	{
		var pierce = Physical;
		var validAttack = new AttackDefinition
		{
			Name = "Claw",
			AbilityScore = AbilityScore.Might,
			AddAbilityScoreToDamage = false,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
					0,
					pierce),
			],
		};
		return new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = hp,
					Definition = new MonsterDefinition
					{
						Id = "m1",
						Name = "Test Beast",
						MaxHp = hp,
						Defense = 10,
						AbilityScores = new AbilityScores(),
						Attacks = [validAttack],
					},
				},
			],
		};
	}

	[Fact]
	public void ExecuteMonsterTurn_InvalidIndex_ReturnsEarly()
	{
		var dice = new SequentialDiceRoll();
		var turn = CreateMonsterTurn(dice);
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var feature = LivingMonsterFixture();

		turn.ExecuteMonsterTurn(session, feature, monsterIndex: 99);

		Assert.Empty(session.LogEntries);
		Assert.Equal(10, session.Player.CurrentHp);
	}

	[Fact]
	public void ExecuteMonsterTurn_DeadMonster_ReturnsEarly()
	{
		var dice = new SequentialDiceRoll();
		var turn = CreateMonsterTurn(dice);
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var feature = LivingMonsterFixture();
		feature.Monsters[0].CurrentHp = 0;

		turn.ExecuteMonsterTurn(session, feature, monsterIndex: 0);

		Assert.Empty(session.LogEntries);
	}

	[Fact]
	public void ExecuteMonsterTurn_NoValidAttacks_LogsDebugAndHesitates()
	{
		var emptyAttack = new AttackDefinition
		{
			Name = "Broken",
			DamageComponents = [],
		};
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 5,
					Definition = new MonsterDefinition
					{
						Id = "no_atk",
						Name = "Sluggish",
						MaxHp = 5,
						Attacks = [emptyAttack],
					},
				},
			],
		};
		var dice = new SequentialDiceRoll();
		var turn = CreateMonsterTurn(dice);
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;

		turn.ExecuteMonsterTurn(session, feature, 0);

		Assert.Contains(session.LogEntries, e => e.Kind == LogEntryKind.Debug && e.Text.Contains("no valid attacks", StringComparison.Ordinal));
		Assert.Contains(session.LogEntries, e => e.Text.Contains("hesitates", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ExecuteMonsterTurn_SkipsFirstInvalidAttack_UsesSecond()
	{
		var pierce = Physical;
		var invalid = new AttackDefinition { Name = "Empty", DamageComponents = [] };
		var valid = new AttackDefinition
		{
			Name = "Real",
			AbilityScore = AbilityScore.Might,
			AddAbilityScoreToDamage = false,
			DamageComponents =
			[
				new DamageComponent(
					new DiceExpression { NumberOfDice = 1, DieType = DieType.d4, InD20CheckPool = false },
					0,
					pierce),
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
						Id = "two_atk",
						Name = "Dual",
						MaxHp = 10,
						AbilityScores = new AbilityScores(),
						Attacks = [invalid, valid],
					},
				},
			],
		};
		var dice = new SequentialDiceRoll(
			Roll(total: 18, resolvedD20: 18),
			Roll(total: 3, resolvedD20: null));
		var turn = CreateMonsterTurn(dice);
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Player.CurrentHp = 50;

		turn.ExecuteMonsterTurn(session, feature, 0);

		Assert.Equal(47, session.Player.CurrentHp);
		Assert.Contains(session.LogEntries,
			e => e.Kind == LogEntryKind.Normal && e.Text.Contains("hits you for 3 damage", StringComparison.OrdinalIgnoreCase));
		Assert.DoesNotContain(session.LogEntries, e => e.Text.Contains("hesitates", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ExecuteMonsterTurn_OnMiss_AppendsMissLineOnly()
	{
		var feature = LivingMonsterFixture();
		var dice = new SequentialDiceRoll(Roll(total: 5, resolvedD20: 5));
		var turn = CreateMonsterTurn(dice);
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Player.CurrentHp = 20;

		turn.ExecuteMonsterTurn(session, feature, 0);

		Assert.Contains(session.LogEntries, e => e.Kind == LogEntryKind.Roll);
		Assert.Contains(session.LogEntries, e => e.Text.Contains("miss", StringComparison.OrdinalIgnoreCase));
		Assert.Equal(20, session.Player.CurrentHp);
	}

	[Fact]
	public void ExecuteMonsterTurn_OnHit_ReducesPlayerHp()
	{
		var feature = LivingMonsterFixture();
		var dice = new SequentialDiceRoll(
			Roll(total: 20, resolvedD20: 15),
			Roll(total: 4, resolvedD20: null));
		var turn = CreateMonsterTurn(dice);
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Player.CurrentHp = 50;

		turn.ExecuteMonsterTurn(session, feature, 0);

		Assert.Equal(46, session.Player.CurrentHp);
		Assert.Contains(session.LogEntries, e => e.Kind == LogEntryKind.Normal && e.Text.Contains("damage", StringComparison.OrdinalIgnoreCase));
	}
}
