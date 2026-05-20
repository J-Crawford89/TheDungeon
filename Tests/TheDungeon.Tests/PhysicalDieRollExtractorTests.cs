using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TheDungeon.Tests;

public sealed class PhysicalDieRollExtractorTests
{
	[Fact]
	public void FromDiceRollResult_d100_EmitsPercentilePair()
	{
		var roll = new DiceRollResult
		{
			ExpressionResults = new List<DiceExpressionResult>
			{
				new()
				{
					Expression = new DiceExpression { DieType = DieType.d100, NumberOfDice = 2 },
					Rolls = new List<DieRollResult>
					{
						new() { DieType = DieType.d10, RolledValue = 20 },
						new() { DieType = DieType.d10, RolledValue = 4 },
					},
				},
			},
		};

		var specs = PhysicalDieRollExtractor.FromDiceRollResult(
			roll, new DiceRollRequest(), DieRollVisualKind.Player);

		Assert.Equal(2, specs.Count);
		Assert.Equal(DieVisualRole.PercentileTens, specs[0].Role);
		Assert.Equal(20, specs[0].FaceValue);
		Assert.Equal(DieVisualRole.Standard, specs[1].Role);
		Assert.Equal(4, specs[1].FaceValue);
	}

	[Fact]
	public void FromDiceRollResult_SingleD6_EmitsOneSpec()
	{
		var roll = new DiceRollResult
		{
			ExpressionResults = new List<DiceExpressionResult>
			{
				new()
				{
					Expression = new DiceExpression { DieType = DieType.d6, NumberOfDice = 1 },
					Rolls = new List<DieRollResult> { new() { DieType = DieType.d6, RolledValue = 3 } },
				},
			},
		};

		var specs = PhysicalDieRollExtractor.FromDiceRollResult(
			roll, new DiceRollRequest(), DieRollVisualKind.Monster);

		var spec = Assert.Single(specs);
		Assert.Equal(DieRollVisualKind.Monster, spec.Kind);
		Assert.Equal(3, spec.FaceValue);
	}
}
