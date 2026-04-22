using System;

public static class CombatFormulas
{
	public static int PlayerEvasionClass(int agilityScore) => 10 + agilityScore;

	public static int HalveD6Roll(int d6Roll) => (int)Math.Ceiling(d6Roll / 2.0);

	/// <summary>Unarmed strike damage (half d6 + Might); always at least 1 before critical multiplier.</summary>
	public static int UnarmedDamageTotal(int d6Roll, int mightScore)
	{
		var raw = HalveD6Roll(d6Roll) + mightScore;
		return Math.Max(1, raw);
	}
}
