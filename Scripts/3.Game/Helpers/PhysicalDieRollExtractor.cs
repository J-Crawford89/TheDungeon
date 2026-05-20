using System.Collections.Generic;

public static class PhysicalDieRollExtractor
{
	public static IReadOnlyList<PhysicalDieRollSpec> FromDiceRollResult(
		DiceRollResult roll,
		DiceRollRequest request,
		DieRollVisualKind kind)
	{
		var specs = new List<PhysicalDieRollSpec>();
		if (roll.ExpressionResults == null)
			return specs;

		foreach (var expression in roll.ExpressionResults)
		{
			if (expression.Rolls == null)
				continue;

			var dieType = expression.Expression.DieType;
			if (dieType == DieType.d100)
			{
				var total = SumDice(expression.Rolls);
				var faces = PercentileDiceFaceMapper.Map(total);
				specs.Add(new PhysicalDieRollSpec
				{
					Kind = kind,
					DieType = DieType.d10,
					Role = DieVisualRole.PercentileTens,
					FaceValue = faces.PercentileTensFace,
				});
				specs.Add(new PhysicalDieRollSpec
				{
					Kind = kind,
					DieType = DieType.d10,
					Role = DieVisualRole.Standard,
					FaceValue = faces.OnesDieFace,
				});
				continue;
			}

			foreach (var dieRoll in expression.Rolls)
			{
				specs.Add(new PhysicalDieRollSpec
				{
					Kind = kind,
					DieType = dieRoll.DieType,
					Role = DieVisualRole.Standard,
					FaceValue = dieRoll.RolledValue,
				});
			}
		}

		return specs;
	}

	private static int SumDice(IReadOnlyList<DieRollResult> rolls)
	{
		var sum = 0;
		foreach (var r in rolls)
			sum += r.RolledValue;
		return sum;
	}
}
