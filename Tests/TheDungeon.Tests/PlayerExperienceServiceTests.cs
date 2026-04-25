using Xunit;

public sealed class PlayerExperienceServiceTests
{
	[Fact]
	public void GrantExperience_ZeroAmount_NoStateOrLogChange()
	{
		var session = new GameSessionState();
		session.Player.Experience = 12;
		var service = new PlayerExperienceService(new NarrativeService());

		service.GrantExperience(session, 0);

		Assert.Equal(12, session.Player.Experience);
		Assert.Empty(session.LogEntries);
	}

	[Fact]
	public void GrantExperience_PositiveAmount_IncreasesExperience_AndLogsGain()
	{
		var session = new GameSessionState();
		session.Player.Experience = 2;
		var service = new PlayerExperienceService(new NarrativeService());

		service.GrantExperience(session, 5);

		Assert.Equal(7, session.Player.Experience);
		Assert.Single(session.LogEntries);
		Assert.Contains("gain 5 experience", session.LogEntries[0].Text);
		Assert.Contains("Total XP: 7", session.LogEntries[0].Text);
	}

	[Fact]
	public void GrantExperience_NegativeAmount_DecreasesExperience_AndLogsLoss()
	{
		var session = new GameSessionState();
		session.Player.Experience = 10;
		var service = new PlayerExperienceService(new NarrativeService());

		service.GrantExperience(session, -3);

		Assert.Equal(7, session.Player.Experience);
		Assert.Single(session.LogEntries);
		Assert.Contains("lose 3 experience", session.LogEntries[0].Text);
		Assert.Contains("Total XP: 7", session.LogEntries[0].Text);
	}
}
