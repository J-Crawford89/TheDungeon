#nullable enable
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class ProficiencyGrantMapper
{
	public static ProficiencyGrant? ToGrant(ProficiencyGrantResource? resource)
	{
		if (resource == null)
			return null;

		if (resource.Target == null)
		{
			GD.PushWarning("ProficiencyGrantResource: Target is null; grant skipped.");
			return null;
		}

		var id = resource.Target.Id?.Trim() ?? "";
		if (id.Length == 0)
		{
			GD.PushWarning("ProficiencyGrantResource: Target has empty Id; grant skipped.");
			return null;
		}

		return new ProficiencyGrant
		{
			Key = new ProficiencyKey(resource.Target.TargetType, id),
			Rank = resource.Rank,
		};
	}

	public static List<ProficiencyGrant> ToDomainList(Array<ProficiencyGrantResource>? arr)
	{
		var list = new List<ProficiencyGrant>();
		if (arr == null || arr.Count == 0)
			return list;

		foreach (var entry in arr)
		{
			var g = ToGrant(entry);
			if (g != null)
				list.Add(g);
		}

		return list;
	}
}
