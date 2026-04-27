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

	/// <summary>
	/// Optional. When set, to-hit uses only this proficiency key (any <see cref="ProficiencyTargetResource"/> subtype with non-empty Id).
	/// Leave unset for normal behavior: players use unarmed resolver; monsters use unarmed fallback unless you need weapon taxonomy (e.g. assign a <see cref="WeaponGroupResource"/>).
	/// </summary>
	[Export] public ProficiencyTargetResource? ProficiencyLookupOverrideTarget { get; set; }
}
