#nullable enable
using System.Collections.Generic;
using Godot.Collections;

public static class LevelAbilityGrantMapper
{
	public static List<LevelAbilityGrant> ToDomainList(Array<LevelAbilityGrantResource>? grants)
	{
		if (grants == null || grants.Count == 0)
			return [];
		var list = new List<LevelAbilityGrant>();
		foreach (var r in grants)
		{
			if (r == null)
				continue;
			var id = r.AbilityId?.Trim() ?? "";
			if (string.IsNullOrEmpty(id))
				continue;
			list.Add(new LevelAbilityGrant { Level = r.Level, AbilityId = id });
		}

		return list;
	}
}
