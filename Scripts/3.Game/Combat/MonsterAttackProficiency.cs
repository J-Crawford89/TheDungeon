#nullable enable

/// <summary>Which proficiency bonus applies to a monster attack (override vs unarmed-style fallback).</summary>
public static class MonsterAttackProficiency
{
	public static WeaponProficiencyResolution Resolve(MonsterDefinition definition, AttackDefinition attack)
	{
		var ov = attack.ProficiencyLookupOverride;
		if (ov.HasValue)
			return ProficiencyLookupResolution.ResolveSingleKey(definition.Proficiencies, ov.Value);

		return UnarmedProficiencyResolver.Resolve(definition.Proficiencies, attack.Id);
	}
}
