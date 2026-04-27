/// <summary>Human-readable proficiency lines for modifiers when no <see cref="WeaponDefinition"/> context exists.</summary>
public static class ProficiencyModifierLabels
{
	public static string ForProficiencyKey(ProficiencyKey key)
	{
		if (key.TargetType == ProficiencyTargetType.UnarmedStrikes &&
		    string.Equals(key.TargetId, UnarmedProficiencyIds.All, System.StringComparison.OrdinalIgnoreCase))
			return "Unarmed Strikes Proficiency";

		var name = DisplayIdHelper.FormatSnakeOrRawId(key.TargetId);
		return $"{name} Proficiency";
	}
}
