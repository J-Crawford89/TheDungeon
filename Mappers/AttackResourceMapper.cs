#nullable enable
using System.Collections.Generic;
using Godot.Collections;

public static class AttackResourceMapper
{
	private static ProficiencyKey? MapProficiencyOverride(ProficiencyTargetResource? target)
	{
		if (target == null)
			return null;
		var id = target.Id?.Trim() ?? "";
		if (id.Length == 0)
			return null;
		return new ProficiencyKey(target.TargetType, id);
	}

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
			ProficiencyLookupOverride = MapProficiencyOverride(resource.ProficiencyLookupOverrideTarget),
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
