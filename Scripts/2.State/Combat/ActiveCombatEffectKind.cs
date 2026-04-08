/// <summary>
/// Kinds of transient combat effects. New effect types require new enum members and any <c>switch</c> on the kind must be updated.
/// For a full open-extension pipeline (e.g. <c>IIncomingDamageEffect</c>), wait until several non-trivial modifiers exist.
/// </summary>
public enum ActiveCombatEffectKind
{
	DefendNegateNextNonZeroDamage,
}
