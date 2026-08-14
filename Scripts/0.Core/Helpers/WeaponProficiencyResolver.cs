using System.Collections.Generic;

public static class WeaponProficiencyResolver
{
	/// <summary>
	/// Narrower scope wins on rank ties: exact weapon, then subgroup, group, subcategory, category.
	/// </summary>
	private static readonly ProficiencyTargetType[] SpecificityOrder =
	{
		ProficiencyTargetType.Weapon,
		ProficiencyTargetType.WeaponSubGroup,
		ProficiencyTargetType.WeaponGroup,
		ProficiencyTargetType.WeaponSubCategory,
		ProficiencyTargetType.WeaponCategory,
	};

	public static WeaponProficiencyResolution Resolve(
		IReadOnlyDictionary<ProficiencyKey, ProficiencyRank> proficiencies,
		WeaponDefinition weapon)
	{
		var bestRank = int.MinValue;
		var bestSpecificity = int.MaxValue;
		ProficiencyKey? bestKey = null;

		foreach (var (key, spec) in EnumerateCandidateKeys(weapon))
		{
			if (!proficiencies.TryGetValue(key, out var rank))
				continue;

			var rankValue = (int)rank;
			if (rankValue > bestRank || (rankValue == bestRank && spec < bestSpecificity))
			{
				bestRank = rankValue;
				bestSpecificity = spec;
				bestKey = key;
			}
		}

		if (bestKey == null)
		{
			return new WeaponProficiencyResolution(
				ProficiencyRank.Untrained,
				null,
				"");
		}

		var label = BuildModifierSourceLabel(weapon, bestKey.Value);
		return new WeaponProficiencyResolution((ProficiencyRank)bestRank, bestKey, label);
	}

	private static IEnumerable<(ProficiencyKey Key, int Specificity)> EnumerateCandidateKeys(WeaponDefinition weapon)
	{
		for (var i = 0; i < SpecificityOrder.Length; i++)
		{
			var type = SpecificityOrder[i];
			var id = type switch
			{
				ProficiencyTargetType.Weapon => weapon.Id?.Trim() ?? "",
				ProficiencyTargetType.WeaponSubGroup => weapon.SubGroup.Id?.Trim() ?? "",
				ProficiencyTargetType.WeaponGroup => weapon.Group.Id?.Trim() ?? "",
				ProficiencyTargetType.WeaponSubCategory => weapon.SubCategory.Id?.Trim() ?? "",
				ProficiencyTargetType.WeaponCategory => weapon.Category.Id?.Trim() ?? "",
				_ => "",
			};

			if (id.Length == 0)
				continue;

			yield return (new ProficiencyKey(type, id), i);
		}
	}

	private static string BuildModifierSourceLabel(WeaponDefinition weapon, ProficiencyKey winningKey)
	{
		var name = winningKey.TargetType switch
		{
			ProficiencyTargetType.Weapon => string.IsNullOrWhiteSpace(weapon.Name)
				? DisplayIdHelper.FormatSnakeOrRawId(weapon.Id)
				: weapon.Name.Trim(),
			ProficiencyTargetType.WeaponCategory => PickNamed(weapon.Category.Name, weapon.Category.Id),
			ProficiencyTargetType.WeaponSubCategory => PickNamed(weapon.SubCategory.Name, weapon.SubCategory.Id),
			ProficiencyTargetType.WeaponGroup => PickNamed(weapon.Group.Name, weapon.Group.Id),
			ProficiencyTargetType.WeaponSubGroup => PickNamed(weapon.SubGroup.Name, weapon.SubGroup.Id),
			_ => DisplayIdHelper.FormatSnakeOrRawId(winningKey.TargetId),
		};

		if (string.IsNullOrWhiteSpace(name))
			name = DisplayIdHelper.FormatSnakeOrRawId(winningKey.TargetId);

		return $"{name} Proficiency";
	}

	private static string PickNamed(string? name, string id)
	{
		if (!string.IsNullOrWhiteSpace(name))
			return name.Trim();
		return DisplayIdHelper.FormatSnakeOrRawId(id);
	}
}
