using System;

public static class CombatFormulas
{
	public static int PlayerEvasionClass(int agilityScore) => 10 + agilityScore;

	public static int HalveD6Roll(int d6Roll) => (int)Math.Ceiling(d6Roll / 2.0);

	public static int UnarmedDamageTotal(int d6Roll, int mightScore) => HalveD6Roll(d6Roll) + mightScore;
}
