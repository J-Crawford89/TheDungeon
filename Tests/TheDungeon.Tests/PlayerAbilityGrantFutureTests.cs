using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public sealed class PlayerAbilityGrantFutureTests
{
	[Fact]
	public void EnumerateFutureAbilities_DedupesByAbilityId_UsesMinGrantLevelAcrossSources()
	{
		var warrior = new CharacterClassDefinition
		{
			Id = "warrior",
			AbilityGrants =
			[
				new LevelAbilityGrant { Level = 1, AbilityId = "granted_only" },
				new LevelAbilityGrant { Level = 5, AbilityId = "dup" }
			]
		};
		var elf = new CharacterRaceDefinition
		{
			Id = "elf",
			AbilityGrants = [new LevelAbilityGrant { Level = 3, AbilityId = "dup" }]
		};
		var noble = new CharacterBackgroundDefinition
		{
			Id = "noble",
			AbilityGrants = [new LevelAbilityGrant { Level = 9, AbilityId = "bg_only" }]
		};

		var classes = new ClassListRepo([warrior]);
		var races = new RaceListRepo([elf]);
		var backgrounds = new BackgroundListRepo([noble]);

		var abilities = new MemoryAbilityRepo(
		[
			new AbilityDefinition { Id = "granted_only", Name = "G", Description = "" },
			new AbilityDefinition { Id = "dup", Name = "Dup", Description = "d" },
			new AbilityDefinition { Id = "bg_only", Name = "Bg", Description = "" }
		]);

		var granted = new[] { "granted_only" };

		var rows = PlayerAbilityGrantBuilder.EnumerateFutureAbilitiesNotYetGranted(new FutureAbilityEnumerationRequest
		{
			ClassDefinitionId = "warrior",
			RaceDefinitionId = "elf",
			BackgroundDefinitionId = "noble",
			CharacterClasses = classes,
			CharacterRaces = races,
			CharacterBackgrounds = backgrounds,
			GrantedAbilityIds = granted,
			AbilityDefinitions = abilities
		});

		Assert.Equal(2, rows.Count);
		var dup = rows.Single(r => r.AbilityId == "dup");
		Assert.Equal(3, dup.MinGrantLevel);
		Assert.Contains(rows, r => r.AbilityId == "bg_only" && r.MinGrantLevel == 9);
	}

	[Fact]
	public void EnumerateFutureAbilities_SkipsUnknownAbilityIds()
	{
		var cls = new CharacterClassDefinition
		{
			Id = "c",
			AbilityGrants = [new LevelAbilityGrant { Level = 1, AbilityId = "known" }, new LevelAbilityGrant { Level = 2, AbilityId = "missing" }]
		};
		var classes = new ClassListRepo([cls]);
		var races = new RaceListRepo([]);
		var backgrounds = new BackgroundListRepo([]);
		var abilities = new MemoryAbilityRepo([new AbilityDefinition { Id = "known", Name = "K", Description = "" }]);

		var rows = PlayerAbilityGrantBuilder.EnumerateFutureAbilitiesNotYetGranted(new FutureAbilityEnumerationRequest
		{
			ClassDefinitionId = "c",
			RaceDefinitionId = "",
			BackgroundDefinitionId = "",
			CharacterClasses = classes,
			CharacterRaces = races,
			CharacterBackgrounds = backgrounds,
			GrantedAbilityIds = Array.Empty<string>(),
			AbilityDefinitions = abilities
		});

		Assert.Single(rows);
		Assert.Equal("known", rows[0].AbilityId);
	}

	[Fact]
	public void ApplyToPlayer_SetsCharacterDefinitionIdsFromSelections()
	{
		var random = new Random(1);
		var dice = new DiceRollService(random);
		var abilities = new MemoryAbilityRepo([]);
		var items = new MemoryItemRepo();
		var svc = new CharacterCreationService(dice, random, abilities, items, TestPlayerProficiencyAggregation.CreateEmpty());

		var state = new CharacterCreationState
		{
			Name = "Test",
			FinalAbilityScores = new AbilityScores { Constitution = 0 },
			SelectedRace = new CharacterRaceDefinition { Id = "race1", Name = "R", BaseHp = 1 },
			SelectedClass = new CharacterClassDefinition { Id = "class1", Name = "C", BaseHp = 2 },
			SelectedBackground = new CharacterBackgroundDefinition { Id = "bg1", Name = "B", StartingCoinPurse = new CoinPurse { Copper = 3 } }
		};

		var player = new PlayerState();
		svc.ApplyToPlayer(state, player);

		Assert.Equal("race1", player.CharacterRaceId);
		Assert.Equal("class1", player.CharacterClassId);
		Assert.Equal("bg1", player.CharacterBackgroundId);
	}

	private sealed class ClassListRepo : ICharacterClassDefinitionRepository
	{
		private readonly IReadOnlyList<CharacterClassDefinition> _all;

		public ClassListRepo(IReadOnlyList<CharacterClassDefinition> all) => _all = all;

		public IReadOnlyList<CharacterClassDefinition> All => _all;
	}

	private sealed class RaceListRepo : ICharacterRaceDefinitionRepository
	{
		private readonly IReadOnlyList<CharacterRaceDefinition> _all;

		public RaceListRepo(IReadOnlyList<CharacterRaceDefinition> all) => _all = all;

		public IReadOnlyList<CharacterRaceDefinition> All => _all;
	}

	private sealed class BackgroundListRepo : ICharacterBackgroundDefinitionRepository
	{
		private readonly IReadOnlyList<CharacterBackgroundDefinition> _all;

		public BackgroundListRepo(IReadOnlyList<CharacterBackgroundDefinition> all) => _all = all;

		public IReadOnlyList<CharacterBackgroundDefinition> All => _all;
	}

	private sealed class MemoryAbilityRepo : IAbilityDefinitionRepository
	{
		private readonly Dictionary<string, AbilityDefinition> _byId;

		public MemoryAbilityRepo(IEnumerable<AbilityDefinition> defs)
		{
			_byId = defs.ToDictionary(d => d.Id, StringComparer.Ordinal);
		}

		public IReadOnlyList<AbilityDefinition> All => _byId.Values.ToList();

		public AbilityDefinition? TryGetById(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;
			return _byId.TryGetValue(id.Trim(), out var d) ? d : null;
		}
	}

	private sealed class MemoryItemRepo : IItemDefinitionRepository
	{
		public IReadOnlyList<ItemDefinition> All => Array.Empty<ItemDefinition>();

		public ItemDefinition? TryGetById(string id) => null;

		public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition => Array.Empty<T>();
	}
}
