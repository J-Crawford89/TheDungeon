#nullable enable

public static class AbilityScoresCopy
{
	public static AbilityScores From(AbilityScores source) =>
		new()
		{
			Might = source.Might,
			Constitution = source.Constitution,
			Dexterity = source.Dexterity,
			Agility = source.Agility,
			Intelligence = source.Intelligence,
			Wisdom = source.Wisdom,
			Gravitas = source.Gravitas,
			Luck = source.Luck,
		};
}
