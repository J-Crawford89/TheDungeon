using System.Collections.Generic;

public static class UnarmedProficiencyResolver
{
	public static WeaponProficiencyResolution Resolve(
		IReadOnlyDictionary<ProficiencyKey, ProficiencyRank> proficiencies,
		string? attackDefinitionId)
	{
		var trimmed = attackDefinitionId?.Trim() ?? "";
		ProficiencyRank? bestRank = null;
		ProficiencyKey? bestKey = null;
		var bestIsSpecific = false;

		void Consider(in ProficiencyKey key, bool isSpecific)
		{
			if (!proficiencies.TryGetValue(key, out var rank))
				return;
			if ((int)rank == 0)
				return;

			var rv = (int)rank;
			var prevRv = bestRank.HasValue ? (int)bestRank.Value : int.MinValue;
			if (rv > prevRv || (rv == prevRv && isSpecific && !bestIsSpecific))
			{
				bestRank = rank;
				bestKey = key;
				bestIsSpecific = isSpecific;
			}
		}

		if (trimmed.Length > 0)
			Consider(new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, trimmed), true);

		Consider(new ProficiencyKey(ProficiencyTargetType.UnarmedStrikes, UnarmedProficiencyIds.All), false);

		if (bestKey == null || !bestRank.HasValue)
			return new WeaponProficiencyResolution(ProficiencyRank.Untrained, null, "");

		var label = LabelForWinningUnarmedKey(bestKey.Value);
		return new WeaponProficiencyResolution(bestRank.Value, bestKey, label);
	}

	private static string LabelForWinningUnarmedKey(ProficiencyKey key)
	{
		if (string.Equals(key.TargetId, UnarmedProficiencyIds.All, System.StringComparison.OrdinalIgnoreCase))
			return "Unarmed Strikes Proficiency";

		var idDisplay = DisplayIdHelper.FormatSnakeOrRawId(key.TargetId);
		return $"{idDisplay} Unarmed Strikes Proficiency";
	}
}
