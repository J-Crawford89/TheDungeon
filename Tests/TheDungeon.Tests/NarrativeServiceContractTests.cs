using System;
using Xunit;

public sealed class NarrativeServiceContractTests
{
	[Fact]
	public void ForCombatVictory_ReturnsStableVictoryLine()
	{
		var narrative = new NarrativeService();

		var line = narrative.ForCombatVictory();

		Assert.Equal("You are victorious! The threats here are finished.", line);
	}

	[Fact]
	public void ForGameOverTitle_ReturnsGameOver()
	{
		var narrative = new NarrativeService();

		Assert.Equal("Game Over", narrative.ForGameOverTitle());
	}

	[Fact]
	public void ForGameOverBody_MonsterBranch_ContainsDisplayNameAndSlain()
	{
		var narrative = new NarrativeService();
		var source = new PlayerDamageSource { Type = DamageSourceType.Monster, DisplayName = "Test Goblin" };

		var body = narrative.ForGameOverBody(source);

		Assert.Contains("Test Goblin", body);
		Assert.Contains("slain", body, StringComparison.OrdinalIgnoreCase);
	}
}
