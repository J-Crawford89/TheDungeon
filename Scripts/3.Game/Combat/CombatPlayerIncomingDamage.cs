#nullable enable

public static class CombatPlayerIncomingDamage
{
	public static bool TryApplyDefendNegate(ref int damage, GameSessionState session, NarrativeService narrative)
	{
		if (session.Combat is not { } c)
			return false;
		if (damage <= 0)
			return false;
		var effects = c.ActiveCombatEffects;
		var idx = effects.IndexOf(ActiveCombatEffectKind.DefendNegateNextNonZeroDamage);
		if (idx < 0)
			return false;
		effects.RemoveAt(idx);
		damage = 0;
		session.AppendGameLog(narrative.ForDefendAbsorbedHit());
		return true;
	}
}
