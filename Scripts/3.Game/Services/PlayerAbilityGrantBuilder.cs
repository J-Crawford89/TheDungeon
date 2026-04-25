#nullable enable
using System;
using System.Collections.Generic;

public static class PlayerAbilityGrantBuilder
{
	public static List<GrantedAbility> Build(
		CharacterClassDefinition? cls,
		CharacterRaceDefinition? race,
		CharacterBackgroundDefinition? background,
		int playerLevel,
		IAbilityDefinitionRepository abilityDefinitions)
	{
		var byAbilityId = new Dictionary<string, GrantedAbility>();

		void ConsiderGrants(
			IReadOnlyList<LevelAbilityGrant> grants,
			AbilityGrantSourceType sourceType,
			string sourceDefinitionId)
		{
			foreach (var g in grants)
			{
				if (g.Level > playerLevel)
					continue;
				var rawId = g.AbilityId?.Trim() ?? "";
				if (rawId.Length == 0)
					continue;
				if (abilityDefinitions.TryGetById(rawId) == null)
					continue;

				if (byAbilityId.ContainsKey(rawId))
					continue;

				byAbilityId[rawId] = new GrantedAbility
				{
					AbilityId = rawId,
					SourceType = sourceType,
					SourceId = sourceDefinitionId,
					LevelGranted = g.Level,
					IsTemporary = false,
				};
			}
		}

		if (cls != null)
			ConsiderGrants(cls.AbilityGrants, AbilityGrantSourceType.Class, cls.Id);
		if (race != null)
			ConsiderGrants(race.AbilityGrants, AbilityGrantSourceType.Race, race.Id);
		if (background != null)
			ConsiderGrants(background.AbilityGrants, AbilityGrantSourceType.Background, background.Id);

		var list = new List<GrantedAbility>(byAbilityId.Count);
		foreach (var kv in byAbilityId)
			list.Add(kv.Value);
		list.Sort((a, b) => string.CompareOrdinal(a.AbilityId, b.AbilityId));
		return list;
	}

	/// <summary>
	/// Ability ids listed on class/race/background definitions that are not already granted, validated against
	/// <see cref="FutureAbilityEnumerationRequest.AbilityDefinitions"/>. Duplicates across sources collapse to the minimum listed grant level.
	/// Iteration order matches <see cref="Build"/>: class, then race, then background.
	/// </summary>
	public static List<(string AbilityId, int MinGrantLevel)> EnumerateFutureAbilitiesNotYetGranted(FutureAbilityEnumerationRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		var abilityDefinitions = request.AbilityDefinitions;
		var granted = new HashSet<string>(StringComparer.Ordinal);
		foreach (var id in request.GrantedAbilityIds)
		{
			if (string.IsNullOrWhiteSpace(id))
				continue;
			granted.Add(id.Trim());
		}

		var minLevelByAbility = new Dictionary<string, int>(StringComparer.Ordinal);

		void ConsiderGrants(IReadOnlyList<LevelAbilityGrant> grants)
		{
			foreach (var g in grants)
			{
				var rawId = g.AbilityId?.Trim() ?? "";
				if (rawId.Length == 0)
					continue;
				if (abilityDefinitions.TryGetById(rawId) == null)
					continue;
				if (granted.Contains(rawId))
					continue;

				if (minLevelByAbility.TryGetValue(rawId, out var prev))
				{
					if (g.Level < prev)
						minLevelByAbility[rawId] = g.Level;
				}
				else
					minLevelByAbility[rawId] = g.Level;
			}
		}

		var cls = TryClass(request.CharacterClasses, request.ClassDefinitionId);
		var race = TryRace(request.CharacterRaces, request.RaceDefinitionId);
		var background = TryBackground(request.CharacterBackgrounds, request.BackgroundDefinitionId);

		if (cls != null)
			ConsiderGrants(cls.AbilityGrants);
		if (race != null)
			ConsiderGrants(race.AbilityGrants);
		if (background != null)
			ConsiderGrants(background.AbilityGrants);

		var rows = new List<(string AbilityId, int MinGrantLevel)>(minLevelByAbility.Count);
		foreach (var kv in minLevelByAbility)
			rows.Add((kv.Key, kv.Value));

		rows.Sort((a, b) =>
		{
			var c = a.MinGrantLevel.CompareTo(b.MinGrantLevel);
			if (c != 0)
				return c;
			return string.CompareOrdinal(a.AbilityId, b.AbilityId);
		});
		return rows;
	}

	private static CharacterClassDefinition? TryClass(ICharacterClassDefinitionRepository repo, string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		foreach (var d in repo.All)
		{
			if (d.Id == key)
				return d;
		}

		return null;
	}

	private static CharacterRaceDefinition? TryRace(ICharacterRaceDefinitionRepository repo, string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		foreach (var d in repo.All)
		{
			if (d.Id == key)
				return d;
		}

		return null;
	}

	private static CharacterBackgroundDefinition? TryBackground(ICharacterBackgroundDefinitionRepository repo, string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		foreach (var d in repo.All)
		{
			if (d.Id == key)
				return d;
		}

		return null;
	}
}
