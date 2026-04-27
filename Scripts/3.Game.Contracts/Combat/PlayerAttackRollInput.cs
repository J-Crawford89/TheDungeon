#nullable enable

/// <summary>Shared inputs for building a player attack to-hit request, resolving damage ability, and rolling damage.</summary>
public sealed class PlayerAttackRollInput
{
	public GameSessionState Session { get; set; } = null!;
	public AttackDefinition Attack { get; set; } = null!;
	public WeaponDefinition? Weapon { get; set; }
	public IAttackRollAbilityOverlay? AbilityOverlay { get; set; }
}
