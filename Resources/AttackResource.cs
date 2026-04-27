#nullable enable
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class AttackResource : Resource
{
	[Export] public string Id { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = string.Empty;
	[Export] public int AttackModifier { get; set; }
	[Export] public AbilityScore AbilityScore { get; set; } = AbilityScore.Might;
	[Export] public bool AddAbilityScoreToDamage { get; set; } = true;
	[Export] public Array<DamageComponentResource> DamageComponents { get; set; } = [];
}
