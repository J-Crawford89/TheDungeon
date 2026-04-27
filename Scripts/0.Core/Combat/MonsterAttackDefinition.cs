/// <summary>Monster attack with inline proficiency rank. When <see cref="AttackProficiencyRank"/> is <see cref="ProficiencyRank.Untrained"/>, use <see cref="MonsterDefinition.DefaultAttackProficiencyRank"/>.</summary>
public sealed class MonsterAttackDefinition : AttackDefinition
{
	public ProficiencyRank AttackProficiencyRank { get; set; } = ProficiencyRank.Untrained;
}
