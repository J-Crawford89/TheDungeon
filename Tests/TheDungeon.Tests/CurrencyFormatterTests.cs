using Xunit;

public sealed class CurrencyFormatterTests
{
	[Fact]
	public void DescribeCompact_EmptyPurse_NoCoins()
	{
		Assert.Equal("no coins", CurrencyFormatter.DescribeCompact(new CoinPurse()));
	}

	[Fact]
	public void DescribeCompact_IncludesPositiveDenominationsInOrder()
	{
		var text = CurrencyFormatter.DescribeCompact(new CoinPurse { Platinum = 1, Gold = 2, Copper = 3 });
		Assert.Equal("1 PP, 2 GP, 3 CP", text);
	}

	[Fact]
	public void DescribeSentenceTotal_IncludesCopperEquivalent()
	{
		var text = CurrencyFormatter.DescribeSentenceTotal(new CoinPurse { Copper = 4 });
		Assert.Contains("4 CP", text);
		Assert.Contains("4 cp equivalent", text);
	}
}
