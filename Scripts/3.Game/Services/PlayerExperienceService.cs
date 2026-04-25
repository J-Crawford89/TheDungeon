public sealed class PlayerExperienceService
{
	private readonly NarrativeService _narrative;

	public PlayerExperienceService(NarrativeService narrative) =>
		_narrative = narrative;

	public void GrantExperience(GameSessionState session, int amount)
	{
		if (amount == 0)
			return;

		session.Player.Experience += amount;
		session.AppendGameLog(_narrative.ForExperienceChanged(amount, session.Player.Experience));
	}
}
