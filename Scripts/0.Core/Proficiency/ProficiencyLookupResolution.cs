using System.Collections.Generic;

/// <summary>Single-key proficiency lookups (attack override, feats keyed to one type/id).</summary>
public static class ProficiencyLookupResolution
{
	public static WeaponProficiencyResolution ResolveSingleKey(
		IReadOnlyDictionary<ProficiencyKey, ProficiencyRank> proficiencies,
		ProficiencyKey key)
	{
		if (!proficiencies.TryGetValue(key, out var rank) || (int)rank == 0)
			return new WeaponProficiencyResolution(ProficiencyRank.Untrained, null, "");

		var label = ProficiencyModifierLabels.ForProficiencyKey(key);
		return new WeaponProficiencyResolution(rank, key, label);
	}
}
