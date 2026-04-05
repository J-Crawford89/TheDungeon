using System;

public sealed class GameOverDownedHandler : IPlayerDownedOutcomeHandler
{
	private readonly NarrativeService _narrative;

	public GameOverDownedHandler(NarrativeService narrative)
	{
		_narrative = narrative;
	}

	public int Priority => 1000;

	public bool TryResolve(GameSessionState session, PlayerDownedContext context)
	{
		var v = context.Vitals;
		if (v.HpAfterClamped > 0)
			return false;

		session.LastFallenAdventurer = new FallenAdventurerRecord
		{
			Character = PlayerCharacterSnapshot.From(session.Player),
			DeathFloorLevel = session.Dungeon.CurrentFloor?.Level ?? 0,
			DeathRoomCoord = session.Dungeon.PlayerCoord,
			DamageSource = context.DamageSource,
			HpBeforeLethalBlow = v.HpBefore,
			DamageRequested = v.DamageRequested,
			HypotheticalHpAfter = v.HypotheticalHpAfter,
			OverkillMagnitude = v.OverkillMagnitude,
			UtcTimestamp = DateTime.UtcNow,
		};

		session.Phase = GamePlayPhase.GameOver;
		session.GameOverTitle = _narrative.ForGameOverTitle();
		session.GameOverBody = _narrative.ForGameOverBody(context.DamageSource);

		session.AppendLog(new LogEntry { Kind = LogEntryKind.Important, Text = session.GameOverBody });

		session.Combat = null;
		session.Dungeon.DungeonMode = DungeonMode.Exploration;

		return true;
	}
}
