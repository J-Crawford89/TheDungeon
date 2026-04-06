#nullable enable
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
}
