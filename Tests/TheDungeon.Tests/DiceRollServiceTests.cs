using System;
using System.Collections.Generic;
using Xunit;

public sealed class DiceRollServiceTests
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
	public void Roll_StandardSingleD20Pool_SetsResolvedValue()
	{
		var svc = new DiceRollService(new QueueRandom(20)); // maps to 20 in d20 range
		var req = new DiceRollRequest
		{
			DiceRollLabel = "check",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions =
			[
				new DiceExpression { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true },
			],
			ModifiersWithSources = [],
		};

		var roll = svc.Roll(req);

		Assert.Equal(20, roll.RollTotal);
		Assert.Equal(20, roll.ResolvedD20CheckValue);
	}

	[Fact]
	public void Roll_AdvantageTwoD20Pool_UsesMaxResolvedValue()
	{
		var svc = new DiceRollService(new QueueRandom(2, 18));
		var req = new DiceRollRequest
		{
			DiceRollLabel = "check",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.Advantage,
			DiceExpressions =
			[
				new DiceExpression { NumberOfDice = 2, DieType = DieType.d20, InD20CheckPool = true },
			],
			ModifiersWithSources = [],
		};

		var roll = svc.Roll(req);

		Assert.Equal(18, roll.RollTotal);
		Assert.Equal(18, roll.ResolvedD20CheckValue);
	}

	[Fact]
	public void Roll_DisadvantageTwoD20Pool_UsesMinResolvedValue()
	{
		var svc = new DiceRollService(new QueueRandom(3, 15));
		var req = new DiceRollRequest
		{
			DiceRollLabel = "check",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.Disadvantage,
			DiceExpressions =
			[
				new DiceExpression { NumberOfDice = 2, DieType = DieType.d20, InD20CheckPool = true },
			],
			ModifiersWithSources = [],
		};

		var roll = svc.Roll(req);

		Assert.Equal(3, roll.RollTotal);
		Assert.Equal(3, roll.ResolvedD20CheckValue);
	}

	[Fact]
	public void Roll_AdvantageWithSinglePoolDie_FallsBackToSumWithoutResolvedValue()
	{
		var svc = new DiceRollService(new QueueRandom(13));
		var req = new DiceRollRequest
		{
			DiceRollLabel = "check",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.Advantage,
			DiceExpressions =
			[
				new DiceExpression { NumberOfDice = 1, DieType = DieType.d20, InD20CheckPool = true },
			],
			ModifiersWithSources = [],
		};

		var roll = svc.Roll(req);

		Assert.Equal(13, roll.RollTotal);
		Assert.Null(roll.ResolvedD20CheckValue);
	}

	[Fact]
	public void Roll_StandardWithTwoPoolDice_UsesSumWithoutResolvedValue()
	{
		var svc = new DiceRollService(new QueueRandom(7, 12));
		var req = new DiceRollRequest
		{
			DiceRollLabel = "check",
			TargetNumber = 0,
			CheckStyle = D20CheckStyle.Standard,
			DiceExpressions =
			[
				new DiceExpression { NumberOfDice = 2, DieType = DieType.d20, InD20CheckPool = true },
			],
			ModifiersWithSources = [],
		};

		var roll = svc.Roll(req);

		Assert.Equal(19, roll.RollTotal);
		Assert.Null(roll.ResolvedD20CheckValue);
	}
}
