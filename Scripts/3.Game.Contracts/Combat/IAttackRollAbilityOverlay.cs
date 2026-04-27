#nullable enable

/// <summary>Optional overlay (e.g. class feats, weapon tags) that can override which ability applies. Phase 1: unused.</summary>
public interface IAttackRollAbilityOverlay
{
	void Apply(AttackRollResolutionContext context, ref AbilityScore hitAbility, ref bool addAbilityToDamage);
}
