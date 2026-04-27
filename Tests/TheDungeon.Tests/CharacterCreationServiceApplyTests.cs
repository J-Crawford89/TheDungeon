using System.Collections.Generic;
using Xunit;

public sealed class CharacterCreationServiceApplyTests
{
	private sealed class AbilityRepo : IAbilityDefinitionRepository
	{
		private readonly Dictionary<string, AbilityDefinition> _map;

		public AbilityRepo(params AbilityDefinition[] defs)
		{
			_map = new Dictionary<string, AbilityDefinition>();
			foreach (var d in defs)
				_map[d.Id] = d;
		}

		public IReadOnlyList<AbilityDefinition> All => new List<AbilityDefinition>(_map.Values);

		public AbilityDefinition? TryGetById(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;
			return _map.TryGetValue(id.Trim(), out var d) ? d : null;
		}
	}

	private sealed class EmptyItemRepo : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => [];
		public ItemDefinition? TryGetById(string id) => null;
		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => [];
	}

	[Fact]
	public void ApplyToPlayer_WithSpellcastingAbility_SetsSpellPoints()
	{
		var dice = new DiceRollService(new System.Random(1));
		var repo = new AbilityRepo(new AbilityDefinition { Id = AbilityIds.Spellcasting, Name = "Spellcasting", Description = "" });
		var service = new CharacterCreationService(
			dice,
			new System.Random(1),
			repo,
			new EmptyItemRepo(),
			TestPlayerProficiencyAggregation.CreateEmpty());
		var state = new CharacterCreationState
		{
			Name = "Mage",
			FinalAbilityScores = new AbilityScores(),
			SelectedClass = new CharacterClassDefinition
			{
				Id = "mage",
				BaseHp = 5,
				AbilityGrants = [new LevelAbilityGrant { Level = 1, AbilityId = AbilityIds.Spellcasting }]
			}
		};
		var player = new PlayerState();

		service.ApplyToPlayer(state, player);

		Assert.Equal(10, player.CurrentSpellPoints);
		Assert.Equal(10, player.MaxSpellPoints);
	}
}
