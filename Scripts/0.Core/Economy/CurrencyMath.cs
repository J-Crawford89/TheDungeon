using System;

/// <summary>Exchange rates and purse arithmetic; does not reshape denominations except explicit helpers.</summary>
public static class CurrencyMath
{
	public const int CopperPerSilver = 100;
	public const int SilverPerGold = 100;
	public const int GoldPerPlatinum = 100;

	public const int CopperPerGold = CopperPerSilver * SilverPerGold;
	public const int CopperPerPlatinum = CopperPerGold * GoldPerPlatinum;

	/// <summary>Scalar wealth / afford-check value; does not mutate purses.</summary>
	public static long TotalEquivalentCopper(CoinPurse p) =>
		(long)p.Copper
		+ (long)p.Silver * CopperPerSilver
		+ (long)p.Gold * CopperPerGold
		+ (long)p.Platinum * CopperPerPlatinum;

	public static void AddInPlace(CoinPurse target, CoinPurse grant)
	{
		if (grant == null)
			return;
		target.Copper += grant.Copper;
		target.Silver += grant.Silver;
		target.Gold += grant.Gold;
		target.Platinum += grant.Platinum;
	}

	public static CoinPurse Add(CoinPurse a, CoinPurse b)
	{
		var t = a.Clone();
		AddInPlace(t, b);
		return t;
	}

	/// <summary>True if actual purse stacks cover <paramref name="costCopper"/> (no exchange).</summary>
	public static bool CanAfford(CoinPurse purse, long costCopper) =>
		TotalEquivalentCopper(purse) >= costCopper;
}
