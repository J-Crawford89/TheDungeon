using Xunit;

public sealed class ResolutionServiceTests
{
	[Fact]
	public void ResolveOutcomeAgainstTarget_Natural20_IsCriticalSuccess()
	{
		var svc = new ResolutionService(new DiceRollService(new System.Random(1)));
		var roll = new DiceRollResult
		{
			SummaryText = "x",
			DetailText = "x",
			RollTotal = 1,
			ModifierTotal = 0,
			Total = 1,
			ExpressionResults = [],
			ModifiersWithSources = [],
			ResolvedD20CheckValue = 20,
		};

		Assert.Equal(ResolutionOutcome.CriticalSuccess, svc.ResolveOutcomeAgainstTarget(roll, 100));
	}

	[Fact]
	public void ResolveOutcomeAgainstTarget_Natural1_IsCriticalFail()
	{
		var svc = new ResolutionService(new DiceRollService(new System.Random(1)));
		var roll = new DiceRollResult
		{
			SummaryText = "x",
			DetailText = "x",
			RollTotal = 20,
			ModifierTotal = 0,
			Total = 20,
			ExpressionResults = [],
			ModifiersWithSources = [],
			ResolvedD20CheckValue = 1,
		};

		Assert.Equal(ResolutionOutcome.CriticalFail, svc.ResolveOutcomeAgainstTarget(roll, 1));
	}

	[Fact]
	public void ResolveOutcomeAgainstTarget_NormalSuccessAndFail()
	{
		var svc = new ResolutionService(new DiceRollService(new System.Random(1)));

		var successRoll = new DiceRollResult
		{
			SummaryText = "x",
			DetailText = "x",
			RollTotal = 0,
			ModifierTotal = 0,
			Total = 10,
			ExpressionResults = [],
			ModifiersWithSources = [],
			ResolvedD20CheckValue = null,
		};
		var failRoll = new DiceRollResult
		{
			SummaryText = "x",
			DetailText = "x",
			RollTotal = 0,
			ModifierTotal = 0,
			Total = 4,
			ExpressionResults = [],
			ModifiersWithSources = [],
			ResolvedD20CheckValue = null,
		};

		Assert.Equal(ResolutionOutcome.Success, svc.ResolveOutcomeAgainstTarget(successRoll, 10));
		Assert.Equal(ResolutionOutcome.Fail, svc.ResolveOutcomeAgainstTarget(failRoll, 10));
	}
}
