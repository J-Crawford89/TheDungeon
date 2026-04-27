public class AttackDefinition
{
	public string? Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public int AttackModifier { get; set; }
	public AbilityScore AbilityScore { get; set; } = AbilityScore.Might;
	public bool AddAbilityScoreToDamage { get; set; } = true;
	public List<DamageComponent> DamageComponents { get; set; } = new();
}
