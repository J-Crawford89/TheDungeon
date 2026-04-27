#nullable enable
using Godot;

[GlobalClass]
public partial class MonsterAttackResource : AttackResource
{
	[Export] public ProficiencyRank AttackProficiencyRank { get; set; } = ProficiencyRank.Untrained;
}
