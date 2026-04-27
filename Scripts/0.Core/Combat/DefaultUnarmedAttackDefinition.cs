using System.Collections.Generic;

/// <summary>Built-in unarmed profile. Empty <see cref="AttackDefinition.DamageComponents"/> signals the legacy half-d6 + Might formula in player damage resolution.</summary>
public static class DefaultUnarmedAttackDefinition
{
	public const string Id = "unarmed_strike";

	public static AttackDefinition Create() =>
		new()
		{
			Id = Id,
			Name = "Unarmed Strike",
			AttackModifier = 0,
			AbilityScore = AbilityScore.Might,
			AddAbilityScoreToDamage = true,
			DamageComponents = new List<DamageComponent>(),
		};
}
