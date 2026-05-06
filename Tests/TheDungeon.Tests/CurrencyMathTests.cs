using Xunit;

public sealed class CurrencyMathTests
{
	[Fact]
	public void TotalEquivalentCopper_MixedPurse()
	{
		var p = new CoinPurse { Copper = 1, Silver = 2, Gold = 3, Platinum = 4 };
		Assert.Equal(1L + 200L + 30_000L + 4_000_000L, CurrencyMath.TotalEquivalentCopper(p));
	}

	[Fact]
	public void AddInPlace_AccumulatesStacks()
	{
		var target = new CoinPurse { Copper = 10 };
		CurrencyMath.AddInPlace(target, new CoinPurse { Copper = 5, Silver = 1 });
		Assert.Equal(15, target.Copper);
		Assert.Equal(1, target.Silver);
	}
}
