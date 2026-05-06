using System.Collections.Generic;

/// <summary>Human-readable coin summaries for logs and UI.</summary>
public static class CurrencyFormatter
{
	public static string DescribeCompact(CoinPurse p)
	{
		var parts = new List<string>();
		if (p.Platinum > 0)
			parts.Add($"{p.Platinum} PP");
		if (p.Gold > 0)
			parts.Add($"{p.Gold} GP");
		if (p.Silver > 0)
			parts.Add($"{p.Silver} SP");
		if (p.Copper > 0)
			parts.Add($"{p.Copper} CP");
		return parts.Count == 0 ? "no coins" : string.Join(", ", parts);
	}

	public static string DescribeSentenceGrant(CoinPurse grant) =>
		DescribeCompact(grant);

	public static string DescribeSentenceTotal(CoinPurse purse)
	{
		var compact = DescribeCompact(purse);
		var cp = CurrencyMath.TotalEquivalentCopper(purse);
		return $"{compact} ({cp} cp equivalent)";
	}
}
