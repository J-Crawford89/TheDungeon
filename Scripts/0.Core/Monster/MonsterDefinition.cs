using System.Collections.Generic;

public sealed class MonsterDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public AbilityScores AbilityScores { get; set; } = new();
	public ProficiencyRank DefaultAttackProficiencyRank { get; set; } = ProficiencyRank.Untrained;
    public List<AttackDefinition> Attacks { get; set; } = new();
    public int Defense { get; set; }
    public int ExperienceReward { get; set; }
    public bool IsBoss { get; set; }
    public int RandomizerWeight { get; set; }
}