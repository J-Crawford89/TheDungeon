using Xunit;

public sealed class PlayerVitalsServiceTests
{
	[Fact]
	public void ApplyDamage_ZeroDamage_LeavesHpUnchanged()
	{
		var player = new PlayerState { CurrentHp = 10 };
		var svc = new PlayerVitalsService();

		var result = svc.ApplyDamage(player, 0);

		Assert.Equal(10, player.CurrentHp);
		Assert.Equal(10, result.HpBefore);
		Assert.Equal(10, result.HpAfterClamped);
		Assert.Equal(10, result.HypotheticalHpAfter);
		Assert.Equal(0, result.OverkillMagnitude);
	}

	[Fact]
	public void ApplyDamage_NonLethal_ReducesHpWithoutOverkill()
	{
		var player = new PlayerState { CurrentHp = 10 };
		var svc = new PlayerVitalsService();

		var result = svc.ApplyDamage(player, 3);

		Assert.Equal(7, player.CurrentHp);
		Assert.Equal(7, result.HpAfterClamped);
		Assert.Equal(7, result.HypotheticalHpAfter);
		Assert.Equal(0, result.OverkillMagnitude);
	}

	[Fact]
	public void ApplyDamage_ExactLethal_ClampsToZeroWithoutOverkill()
	{
		var player = new PlayerState { CurrentHp = 6 };
		var svc = new PlayerVitalsService();

		var result = svc.ApplyDamage(player, 6);

		Assert.Equal(0, player.CurrentHp);
		Assert.Equal(0, result.HpAfterClamped);
		Assert.Equal(0, result.HypotheticalHpAfter);
		Assert.Equal(0, result.OverkillMagnitude);
	}

	[Fact]
	public void ApplyDamage_Overkill_ClampsToZeroAndTracksMagnitude()
	{
		var player = new PlayerState { CurrentHp = 5 };
		var svc = new PlayerVitalsService();

		var result = svc.ApplyDamage(player, 12);

		Assert.Equal(0, player.CurrentHp);
		Assert.Equal(-7, result.HypotheticalHpAfter);
		Assert.Equal(7, result.OverkillMagnitude);
	}
}
