using System.Collections.Generic;

/// <summary>Merges grants into a dictionary by max rank per key (skips untrained and empty ids).</summary>
public static class ProficiencyGrantMerge
{
	public static Dictionary<ProficiencyKey, ProficiencyRank> Merge(IEnumerable<ProficiencyGrant> grants)
	{
		var merged = new Dictionary<ProficiencyKey, ProficiencyRank>();
		foreach (var grant in grants)
		{
			if (grant.Rank == ProficiencyRank.Untrained)
				continue;
			var id = grant.Key.TargetId?.Trim() ?? "";
			if (id.Length == 0)
				continue;

			var key = new ProficiencyKey(grant.Key.TargetType, id);
			if (!merged.TryGetValue(key, out var existing) || (int)grant.Rank > (int)existing)
				merged[key] = grant.Rank;
		}

		return merged;
	}
}
