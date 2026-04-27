using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class CharacterCreationServiceRollTests
{
	private sealed class QueueRandom : Random
	{
		private readonly Queue<int> _values;

		public QueueRandom(params int[] values) => _values = new Queue<int>(values);

		public override int Next(int minValue, int maxValue)
		{
			if (_values.Count == 0)
				return minValue;
			var next = _values.Dequeue();
			return Math.Clamp(next, minValue, maxValue - 1);
		}
	}

	private sealed class FixedZeroRandom : Random
	{
		public override int Next(int maxValue) => 0;
	}

	private sealed class EmptyAbilityRepo : IAbilityDefinitionRepository
	{
		public IReadOnlyList<AbilityDefinition> All => [];
		public AbilityDefinition? TryGetById(string id) => null;
	}

	private sealed class EmptyItemRepo : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	private static int Sum(AbilityScores s) =>
		s.Might + s.Constitution + s.Dexterity + s.Agility + s.Intelligence + s.Wisdom + s.Gravitas + s.Luck;

	[Fact]
	public void RollAndApplyRolledScores_ReducesUntilTotalBelowEight_AndCopiesToFinal()
	{
		// Eight d3 rolls all set to 1 => starting sum 8; reduction loop must bring sum below 8.
		var dice = new DiceRollService(new QueueRandom(1, 1, 1, 1, 1, 1, 1, 1));
		var service = new CharacterCreationService(
			dice,
			new FixedZeroRandom(),
			new EmptyAbilityRepo(),
			new EmptyItemRepo(),
			TestPlayerProficiencyAggregation.CreateEmpty());
		var state = new CharacterCreationState();

		service.RollAndApplyRolledScores(state);

		Assert.True(Sum(state.RolledAbilityScores) < 8);
		Assert.Equal(0, state.RolledAbilityScores.Might);
		Assert.Equal(7, Sum(state.FinalAbilityScores));
		Assert.Equal(state.RolledAbilityScores.Might, state.FinalAbilityScores.Might);
		Assert.Equal(state.RolledAbilityScores.Luck, state.FinalAbilityScores.Luck);
	}

	[Fact]
	public void RecomputeFinalAbilityScores_CopiesRolledValues()
	{
		var service = new CharacterCreationService(
			new DiceRollService(new Random(1)),
			new Random(1),
			new EmptyAbilityRepo(),
			new EmptyItemRepo(),
			TestPlayerProficiencyAggregation.CreateEmpty());
		var state = new CharacterCreationState
		{
			RolledAbilityScores = new AbilityScores
			{
				Might = 3,
				Constitution = 2,
				Dexterity = 1,
				Agility = 0,
				Intelligence = -1,
				Wisdom = -2,
				Gravitas = 3,
				Luck = 2,
			},
			FinalAbilityScores = new AbilityScores(),
		};

		service.RecomputeFinalAbilityScores(state);

		Assert.Equal(3, state.FinalAbilityScores.Might);
		Assert.Equal(-2, state.FinalAbilityScores.Wisdom);
		Assert.Equal(2, state.FinalAbilityScores.Luck);
	}
}
