using System.Linq;
using Xunit;

public sealed class DieVisualCatalogKeysTests
{
	[Fact]
	public void GetResolveKeys_StandardDie_ReturnsRequestedPairOnly()
	{
		var keys = DieVisualCatalogKeys.GetResolveKeys(DieType.d20, DieVisualRole.Standard).ToList();
		Assert.Single(keys);
		Assert.Equal((DieType.d20, DieVisualRole.Standard), keys[0]);
	}

	[Fact]
	public void GetResolveKeys_PercentileTens_IncludesD100Fallback()
	{
		var keys = DieVisualCatalogKeys.GetResolveKeys(DieType.d10, DieVisualRole.PercentileTens).ToList();
		Assert.Equal(2, keys.Count);
		Assert.Equal((DieType.d10, DieVisualRole.PercentileTens), keys[0]);
		Assert.Equal((DieType.d100, DieVisualRole.PercentileTens), keys[1]);
	}
}
