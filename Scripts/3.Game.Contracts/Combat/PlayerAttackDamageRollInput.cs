#nullable enable

/// <summary>Inputs for rolling player attack damage (use after resolving damage ability / overlay).</summary>
public sealed class PlayerAttackDamageRollInput
{
	public IDiceRollRequestExecutor Dice { get; set; } = null!;
	public PlayerAttackRollInput Roll { get; set; } = null!;
	public AbilityScore DamageAbility { get; set; }
	public bool AddAbilityToDamage { get; set; }
}
