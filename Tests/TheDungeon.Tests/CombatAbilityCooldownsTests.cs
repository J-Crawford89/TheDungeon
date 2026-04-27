using Xunit;

public sealed class CombatAbilityCooldownsTests
{
	[Fact]
	public void Start_InvalidInputs_AreIgnored()
	{
		var cds = new CombatAbilityCooldowns();
		cds.Start(" ", 2);
		cds.Start("defend", 0);
		cds.Start("defend", -1);

		Assert.False(cds.IsOnCooldown("defend"));
	}

	[Fact]
	public void Start_TrimsId_AndGetRemainingUsesTrimmedLookup()
	{
		var cds = new CombatAbilityCooldowns();
		cds.Start(" defend ", 2);

		Assert.True(cds.IsOnCooldown("defend"));
		Assert.Equal(2, cds.GetRemaining("defend"));
		Assert.Equal(2, cds.GetRemaining(" defend "));
	}

	[Fact]
	public void OnPlayerTurnStarted_CountsDownAndRemovesWhenZero()
	{
		var cds = new CombatAbilityCooldowns();
		cds.Start("defend", 2);

		cds.OnPlayerTurnStarted();
		Assert.Equal(1, cds.GetRemaining("defend"));

		cds.OnPlayerTurnStarted();
		Assert.Equal(0, cds.GetRemaining("defend"));
		Assert.False(cds.IsOnCooldown("defend"));
	}

	[Fact]
	public void OnPlayerTurnStarted_HandlesMultipleAbilities()
	{
		var cds = new CombatAbilityCooldowns();
		cds.Start("defend", 1);
		cds.Start("bash", 3);

		cds.OnPlayerTurnStarted();

		Assert.False(cds.IsOnCooldown("defend"));
		Assert.Equal(2, cds.GetRemaining("bash"));
	}

	[Fact]
	public void Clear_RemovesAllCooldowns()
	{
		var cds = new CombatAbilityCooldowns();
		cds.Start("defend", 2);
		cds.Start("bash", 2);

		cds.Clear();

		Assert.Equal(0, cds.GetRemaining("defend"));
		Assert.Equal(0, cds.GetRemaining("bash"));
	}
}
