public sealed class PlayerCharacterSnapshot
{
	public string Name { get; init; } = "";
	public int MaxHp { get; init; }
	public int Level { get; init; }
	public int Gold { get; init; }
	public int HealthPotionCount { get; init; }
	public HorizontalDirection Facing { get; init; }
	public AbilityScores AbilityScores { get; init; } = new();

	public static PlayerCharacterSnapshot From(PlayerState player)
	{
		var src = player.AbilityScores;
		return new PlayerCharacterSnapshot
		{
			Name = player.Name,
			MaxHp = player.MaxHp,
			Level = player.Level,
			Gold = player.Gold,
			HealthPotionCount = player.HealthPotionCount,
			Facing = player.Facing,
			AbilityScores = new AbilityScores
			{
				Might = src.Might,
				Constitution = src.Constitution,
				Dexterity = src.Dexterity,
				Agility = src.Agility,
				Intelligence = src.Intelligence,
				Wisdom = src.Wisdom,
				Gravitas = src.Gravitas,
				Luck = src.Luck,
			},
		};
	}
}
