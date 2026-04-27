public sealed class AttackDefinition
{
	public string? Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public int AttackModifier { get; set; }
	public AbilityScore AbilityScore { get; set; } = AbilityScore.Might;
	public bool AddAbilityScoreToDamage { get; set; } = true;
	public List<DamageComponent> DamageComponents { get; set; } = new();

	/// <summary>
	/// When set, to-hit uses only this proficiency key (no unarmed-specific fallback).
	/// When null, player unarmed and typical monster attacks use <see cref="UnarmedProficiencyResolver"/>.
	/// </summary>
	public ProficiencyKey? ProficiencyLookupOverride { get; set; }
}
