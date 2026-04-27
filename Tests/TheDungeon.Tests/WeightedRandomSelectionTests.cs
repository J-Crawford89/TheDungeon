using System;
using Xunit;

public sealed class WeightedRandomSelectionTests
{
	[Fact]
	public void Pick_WhenNoPositiveWeights_Throws()
	{
		Assert.Throws<ArgumentException>(() =>
			WeightedRandomSelection.Pick(new Random(1), [("a", 0d), ("b", -1d)]));
	}

	[Fact]
	public void Pick_WithSinglePositiveWeight_ReturnsThatItem()
	{
		var pick = WeightedRandomSelection.Pick(new Random(1), [("a", -1d), ("b", 5d)]);
		Assert.Equal("b", pick);
	}
}
