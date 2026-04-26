using System;
using System.Collections.Generic;
using Xunit;

public sealed class ProficiencyTests
{
	[Fact]
	public void Aggregation_keeps_highest_rank_for_duplicate_key()
	{
		var race = new CharacterRaceDefinition
		{
			Id = "r1",
			ProficiencyGrants =
			[
				new ProficiencyGrant
				{
					Key = new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blades"),
					Rank = ProficiencyRank.Buffoon,
				},
				new ProficiencyGrant
				{
					Key = new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blades"),
					Rank = ProficiencyRank.Trained,
				},
			],
		};

		var svc = new PlayerProficiencyAggregationService(
			new SingleRaceRepo(race),
			new EmptyClassesRepo(),
			new EmptyBgRepo());

		var player = new PlayerState { CharacterRaceId = "r1" };
		svc.Recompute(player);

		var key = new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blades");
		Assert.True(player.Proficiencies.TryGetValue(key, out var rank));
		Assert.Equal(ProficiencyRank.Trained, rank);
	}

	[Fact]
	public void Aggregation_skips_untrained_grants()
	{
		var cls = new CharacterClassDefinition
		{
			Id = "c1",
			ProficiencyGrants =
			[
				new ProficiencyGrant
				{
					Key = new ProficiencyKey(ProficiencyTargetType.WeaponCategory, "basic"),
					Rank = ProficiencyRank.Untrained,
				},
			],
		};

		var svc = new PlayerProficiencyAggregationService(
			new EmptyRacesRepo(),
			new SingleClassRepo(cls),
			new EmptyBgRepo());

		var player = new PlayerState { CharacterClassId = "c1" };
		svc.Recompute(player);

		Assert.Empty(player.Proficiencies);
	}

	[Fact]
	public void Aggregation_weapon_ownership_uses_ownership_rank()
	{
		var sword = new WeaponDefinition
		{
			Id = "arming_sword",
			Name = "Arming Sword",
			OwnershipProficiencyRank = ProficiencyRank.Expert,
		};

		var player = new PlayerState();
		player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = sword,
			Quantity = 1,
			InstanceId = Guid.NewGuid(),
		});

		var svc = new PlayerProficiencyAggregationService(
			new EmptyRacesRepo(),
			new EmptyClassesRepo(),
			new EmptyBgRepo());

		svc.Recompute(player);

		var key = new ProficiencyKey(ProficiencyTargetType.Weapon, "arming_sword");
		Assert.True(player.Proficiencies.TryGetValue(key, out var rank));
		Assert.Equal(ProficiencyRank.Expert, rank);
	}

	[Fact]
	public void Aggregation_untrained_ownership_does_not_add_weapon_key()
	{
		var sword = new WeaponDefinition
		{
			Id = "plain",
			OwnershipProficiencyRank = ProficiencyRank.Untrained,
		};

		var player = new PlayerState();
		player.InventoryState.Items.Add(new ItemInstance
		{
			Definition = sword,
			Quantity = 1,
			InstanceId = Guid.NewGuid(),
		});

		var svc = new PlayerProficiencyAggregationService(
			new EmptyRacesRepo(),
			new EmptyClassesRepo(),
			new EmptyBgRepo());

		svc.Recompute(player);

		Assert.Empty(player.Proficiencies);
	}

	[Fact]
	public void WeaponResolver_picks_highest_rank_across_keys()
	{
		var weapon = new WeaponDefinition
		{
			Id = "sword1",
			Name = "Sword",
			Category = new WeaponCategoryDefinition { Id = "basic", Name = "Basic" },
			Group = new WeaponGroupDefinition { Id = "blades", Name = "Blades" },
		};

		var profs = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blades")] = ProficiencyRank.Buffoon,
			[new ProficiencyKey(ProficiencyTargetType.WeaponCategory, "basic")] = ProficiencyRank.Trained,
		};

		var r = WeaponProficiencyResolver.Resolve(profs, weapon);
		Assert.Equal(ProficiencyRank.Trained, r.Rank);
		Assert.Contains("Basic", r.ModifierSourceLabel, StringComparison.Ordinal);
	}

	[Fact]
	public void WeaponResolver_exact_weapon_beats_lower_group()
	{
		var weapon = new WeaponDefinition
		{
			Id = "sword1",
			Name = "Named Sword",
			Group = new WeaponGroupDefinition { Id = "blades", Name = "Blades" },
		};

		var profs = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.Weapon, "sword1")] = ProficiencyRank.Expert,
			[new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blades")] = ProficiencyRank.Trained,
		};

		var r = WeaponProficiencyResolver.Resolve(profs, weapon);
		Assert.Equal(ProficiencyRank.Expert, r.Rank);
		Assert.Contains("Named Sword", r.ModifierSourceLabel, StringComparison.Ordinal);
	}

	[Fact]
	public void WeaponResolver_same_rank_prefers_more_specific_key()
	{
		var weapon = new WeaponDefinition
		{
			Id = "sword1",
			Name = "Sword",
			SubGroup = new WeaponSubGroupDefinition { Id = "long_blades", Name = "Long Blades" },
			Group = new WeaponGroupDefinition { Id = "blades", Name = "Blades" },
		};

		var profs = new Dictionary<ProficiencyKey, ProficiencyRank>
		{
			[new ProficiencyKey(ProficiencyTargetType.WeaponGroup, "blades")] = ProficiencyRank.Trained,
			[new ProficiencyKey(ProficiencyTargetType.WeaponSubGroup, "long_blades")] = ProficiencyRank.Trained,
		};

		var r = WeaponProficiencyResolver.Resolve(profs, weapon);
		Assert.Equal(ProficiencyRank.Trained, r.Rank);
		Assert.Contains("Long Blades", r.ModifierSourceLabel, StringComparison.Ordinal);
	}

	private sealed class SingleRaceRepo : ICharacterRaceDefinitionRepository
	{
		private readonly CharacterRaceDefinition _race;

		public SingleRaceRepo(CharacterRaceDefinition race) => _race = race;

		public IReadOnlyList<CharacterRaceDefinition> All => [_race];
	}

	private sealed class SingleClassRepo : ICharacterClassDefinitionRepository
	{
		private readonly CharacterClassDefinition _cls;

		public SingleClassRepo(CharacterClassDefinition cls) => _cls = cls;

		public IReadOnlyList<CharacterClassDefinition> All => [_cls];
	}

	private sealed class EmptyClassesRepo : ICharacterClassDefinitionRepository
	{
		public IReadOnlyList<CharacterClassDefinition> All => [];
	}

	private sealed class EmptyRacesRepo : ICharacterRaceDefinitionRepository
	{
		public IReadOnlyList<CharacterRaceDefinition> All => [];
	}

	private sealed class EmptyBgRepo : ICharacterBackgroundDefinitionRepository
	{
		public IReadOnlyList<CharacterBackgroundDefinition> All => [];
	}
}
