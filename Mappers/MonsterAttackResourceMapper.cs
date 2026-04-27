#nullable enable

public static class MonsterAttackResourceMapper
{
	public static MonsterAttackDefinition? ToDomain(MonsterAttackResource resource)
	{
		if (resource == null)
			return null;

		var damageList = DamageComponentsMapper.ToDomainList(resource.DamageComponents);
		if (damageList.Count == 0)
			return null;

		return new MonsterAttackDefinition
		{
			Id = string.IsNullOrWhiteSpace(resource.Id) ? null : resource.Id.Trim(),
			Name = resource.Name,
			AttackModifier = resource.AttackModifier,
			AbilityScore = resource.AbilityScore,
			AddAbilityScoreToDamage = resource.AddAbilityScoreToDamage,
			DamageComponents = damageList,
			AttackProficiencyRank = resource.AttackProficiencyRank,
		};
	}
}
