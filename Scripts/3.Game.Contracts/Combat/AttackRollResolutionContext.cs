#nullable enable

/// <summary>Inputs for resolving which ability applies to an attack; optional overlay for future class abilities / weapon tags.</summary>
/// <remarks>
/// Deferred: weapon eligibility (e.g. Backstab), tag-driven ability swaps, and similar rules plug in via
/// <see cref="IAttackRollAbilityOverlay"/> or a future resolution context extension without changing <see cref="AttackDefinition"/> again.
/// </remarks>
public sealed class AttackRollResolutionContext
{
	public AttackDefinition Attack { get; set; } = null!;
	public WeaponDefinition? Weapon { get; set; }
}
