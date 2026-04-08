using System.Linq;

public sealed class CombatEncounterLifecycle
{
	private readonly NarrativeService _narrative;

	public CombatEncounterLifecycle(NarrativeService narrative) =>
		_narrative = narrative;

	public void EndCombatVictory(GameSessionState session)
	{
		session.AppendGameLog(_narrative.ForCombatVictory());
		session.Combat = null;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
	}

	public void RestoreExplorationAfterFlee(GameSessionState session)
	{
		var combat = session.Combat!;
		session.Dungeon.PlayerCoord = combat.FleeReturnCoord;
		var floor = session.Dungeon.Floors.FirstOrDefault(f => f.Level == combat.FleeReturnFloorLevel);
		if (floor != null)
			session.Dungeon.CurrentFloor = floor;
		session.Combat = null;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
	}
}
