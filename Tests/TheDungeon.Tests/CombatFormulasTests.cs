using Xunit;

public sealed class CombatFormulasTests
{
	[Fact]
	public void HalveD6Roll_OddValues_RoundUp()
	{
		Assert.Equal(1, CombatFormulas.HalveD6Roll(1));
		Assert.Equal(2, CombatFormulas.HalveD6Roll(3));
		Assert.Equal(3, CombatFormulas.HalveD6Roll(5));
	}

	[Fact]
	public void UnarmedDamageTotal_IsAtLeastOne()
	{
		Assert.Equal(1, CombatFormulas.UnarmedDamageTotal(1, -10));
	}

	[Fact]
	public void PlayerArmorThreshold_DoesNotApplyNegativeArmorBonus()
	{
		Assert.Equal(10, CombatFormulas.PlayerArmorThreshold(10, -5));
		Assert.Equal(13, CombatFormulas.PlayerArmorThreshold(10, 3));
	}
}
