#nullable enable
using Godot;

[GlobalClass]
public partial class ProficiencyGrantResource : Resource
{
	[Export] public ProficiencyTargetResource? Target { get; set; }
	[Export] public ProficiencyRank Rank { get; set; } = ProficiencyRank.Untrained;
}
