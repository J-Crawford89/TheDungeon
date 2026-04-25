public sealed class AttackDefinition
{
	public string Name { get; set; } = string.Empty;
	public int AttackModifier { get; set; }
	public ItemDefinition? AttackItem { get; set; }
	public DamageComponent? Damage { get; set; }
}
