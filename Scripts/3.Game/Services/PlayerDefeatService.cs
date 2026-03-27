public sealed class PlayerDefeatService
{
	private readonly NarrativeService _narrative;

	public PlayerDefeatService(NarrativeService narrative)
	{
		_narrative = narrative;
	}

	public void ReturnToDungeonEntrance(GameSessionState session)
	{
		session.AppendGameLog(_narrative.ForPlayerDefeated());
		session.Combat = null;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		if (session.Dungeon.Floors.Count > 0)
			session.Dungeon.CurrentFloor = session.Dungeon.Floors[0];
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
	}
}
