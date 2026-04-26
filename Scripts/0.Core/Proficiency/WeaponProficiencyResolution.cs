public readonly struct WeaponProficiencyResolution
{
	public WeaponProficiencyResolution(ProficiencyRank rank, ProficiencyKey? winningKey, string modifierSourceLabel)
	{
		Rank = rank;
		WinningKey = winningKey;
		ModifierSourceLabel = modifierSourceLabel;
	}

	public ProficiencyRank Rank { get; }
	public ProficiencyKey? WinningKey { get; }
	public string ModifierSourceLabel { get; }
}
