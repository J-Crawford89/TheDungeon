#nullable enable
using System.Collections.Generic;
using Godot.Collections;

public static class AttackResourceMapper
{
	public static AttackDefinition? ToDomain(AttackResource resource)
	{
		if (resource == null)
			return null;

		var damageList = DamageComponentsMapper.ToDomainList(resource.DamageComponents);
		if (damageList.Count == 0)
			return null;

		return new AttackDefinition
		{
			Id = string.IsNullOrWhiteSpace(resource.Id) ? null : resource.Id.Trim(),
			Name = resource.Name,
			AttackModifier = resource.AttackModifier,
			AbilityScore = resource.AbilityScore,
			AddAbilityScoreToDamage = resource.AddAbilityScoreToDamage,
			DamageComponents = damageList,
		};
	}

	public static List<AttackDefinition> ToDomainList(Array<AttackResource>? attacks)
	{
		var list = new List<AttackDefinition>();
		if (attacks == null || attacks.Count == 0)
			return list;

		foreach (var a in attacks)
		{
			var mapped = ToDomain(a);
			if (mapped != null)
				list.Add(mapped);
		}

		return list;
	}
}
