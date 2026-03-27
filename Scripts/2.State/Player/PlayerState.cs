using System;

public sealed class PlayerState
{
	public string Name { get; set; } = "Testy McTestface";
	public int CurrentHp { get; set; } = 10;
	public int MaxHp { get; set; } = 10;
	public int Level { get; set; } = 1;
	public int Gold { get; set; }
	public int HealthPotionCount { get; set; }
	public HorizontalDirection Facing { get; set; } = HorizontalDirection.North;
	public AbilityScores AbilityScores { get; set; } = new();
}
