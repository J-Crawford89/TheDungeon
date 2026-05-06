#nullable enable

public static class CoinPurseMapper
{
	public static CoinPurse ToDomain(CoinPurseResource? resource)
	{
		if (resource == null)
			return new CoinPurse();
		return new CoinPurse
		{
			Copper = resource.Copper,
			Silver = resource.Silver,
			Gold = resource.Gold,
			Platinum = resource.Platinum,
		};
	}
}
