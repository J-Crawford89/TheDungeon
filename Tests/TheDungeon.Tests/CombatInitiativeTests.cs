using System;
using System.Collections.Generic;
using Xunit;

public sealed class CombatInitiativeTests
{
	private sealed class QueueRandom : Random
	{
		private readonly Queue<int> _values;

		public QueueRandom(params int[] values) => _values = new Queue<int>(values);

		public override int Next(int minValue, int maxValue)
		{
			if (_values.Count == 0)
				return minValue;
			var v = _values.Dequeue();
			return Math.Clamp(v, minValue, maxValue - 1);
		}
	}

	[Fact]
	public void InitiativeHelperCompare_TieBreaksPlayerBeforeMonster()
	{
		var player = new InitiativeEntry { Total = 12, Agility = 2, IsPlayer = true, MonsterIndex = -1 };
		var monster = new InitiativeEntry { Total = 12, Agility = 2, IsPlayer = false, MonsterIndex = 0 };

		var cmp = InitiativeHelper.Compare(player, monster);

		Assert.True(cmp < 0);
	}

	[Fact]
	public void RollInitiativeOrder_SkipsDeadMonsters_AndReturnsPlayerPlusAlive()
	{
		var session = new GameSessionState();
		session.Player.AbilityScores.Agility = 0;
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance { CurrentHp = 1, Definition = new MonsterDefinition { Name = "Alive A" } },
				new MonsterInstance { CurrentHp = 0, Definition = new MonsterDefinition { Name = "Dead B" } },
				new MonsterInstance { CurrentHp = 1, Definition = new MonsterDefinition { Name = "Alive C" } },
			]
		};
		// Player roll 10, monster0 roll 5, monster2 roll 15
		var init = new CombatInitiative(new DiceRollService(new QueueRandom(10, 5, 15)), new NarrativeService());

		var order = init.RollInitiativeOrder(session, feature);

		Assert.Equal(3, order.Count);
		Assert.Contains(order, s => s.IsPlayer);
		Assert.DoesNotContain(order, s => !s.IsPlayer && s.MonsterIndex == 1);
	}

	[Fact]
	public void BuildTurnOrderNames_MapsSlotsToNames()
	{
		var feature = new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance { Definition = new MonsterDefinition { Name = "Rat" } },
			]
		};
		var init = new CombatInitiative(new DiceRollService(new Random(1)), new NarrativeService());
		var order = new List<CombatTurnSlot>
		{
			new() { IsPlayer = true },
			new() { IsPlayer = false, MonsterIndex = 0 },
		};

		var names = init.BuildTurnOrderNames(feature, order);

		Assert.Equal(["You", "Rat"], names);
	}
}
