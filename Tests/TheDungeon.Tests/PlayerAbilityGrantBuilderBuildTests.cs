using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class PlayerAbilityGrantBuilderBuildTests
{
	private sealed class AbilityRepo : IAbilityDefinitionRepository
	{
		private readonly Dictionary<string, AbilityDefinition> _byId;

		public AbilityRepo(params AbilityDefinition[] defs) =>
			_byId = defs.ToDictionary(d => d.Id, StringComparer.Ordinal);

		public IReadOnlyList<AbilityDefinition> All => _byId.Values.ToList();

		public AbilityDefinition? TryGetById(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;
			return _byId.TryGetValue(id.Trim(), out var d) ? d : null;
		}
	}

	[Fact]
	public void Build_FiltersByLevelAndKnownAbilities_AndSortsById()
	{
		var cls = new CharacterClassDefinition
		{
			Id = "class_a",
			AbilityGrants =
			[
				new LevelAbilityGrant { Level = 1, AbilityId = "b_ability" },
				new LevelAbilityGrant { Level = 5, AbilityId = "future" },
				new LevelAbilityGrant { Level = 1, AbilityId = "  " },
			]
		};
		var race = new CharacterRaceDefinition
		{
			Id = "race_a",
			AbilityGrants =
			[
				new LevelAbilityGrant { Level = 1, AbilityId = "a_ability" },
				new LevelAbilityGrant { Level = 1, AbilityId = "missing" },
			]
		};
		var defs = new AbilityRepo(
			new AbilityDefinition { Id = "a_ability", Name = "A", Description = "" },
			new AbilityDefinition { Id = "b_ability", Name = "B", Description = "" });

		var grants = PlayerAbilityGrantBuilder.Build(cls, race, null, playerLevel: 1, defs);

		Assert.Equal(2, grants.Count);
		Assert.Equal(["a_ability", "b_ability"], grants.Select(g => g.AbilityId).ToArray());
	}

	[Fact]
	public void Build_DuplicateAbilityAcrossSources_FirstSourceWins()
	{
		var cls = new CharacterClassDefinition
		{
			Id = "class_a",
			AbilityGrants = [new LevelAbilityGrant { Level = 1, AbilityId = "shared" }]
		};
		var race = new CharacterRaceDefinition
		{
			Id = "race_a",
			AbilityGrants = [new LevelAbilityGrant { Level = 1, AbilityId = "shared" }]
		};
		var background = new CharacterBackgroundDefinition
		{
			Id = "bg_a",
			AbilityGrants = [new LevelAbilityGrant { Level = 1, AbilityId = "shared" }]
		};
		var defs = new AbilityRepo(new AbilityDefinition { Id = "shared", Name = "Shared", Description = "" });

		var grants = PlayerAbilityGrantBuilder.Build(cls, race, background, playerLevel: 10, defs);

		Assert.Single(grants);
		Assert.Equal(AbilityGrantSourceType.Class, grants[0].SourceType);
		Assert.Equal("class_a", grants[0].SourceId);
	}
}
