using Xunit;

public sealed class GameOverDownedHandlerTests
{
	[Fact]
	public void TryResolve_WhenHpStillAboveZero_ReturnsFalse()
	{
		var handler = new GameOverDownedHandler(new NarrativeService());
		var session = new GameSessionState();
		var context = new PlayerDownedContext
		{
			Vitals = new VitalsDamageResult { HpBefore = 10, DamageRequested = 1, HypotheticalHpAfter = 9, HpAfterClamped = 9, OverkillMagnitude = 0 },
			DamageSource = new PlayerDamageSource { Type = DamageSourceType.Trap, DisplayName = "Trap" }
		};

		Assert.False(handler.TryResolve(session, context));
		Assert.Equal(GamePlayPhase.InProgress, session.Phase);
	}

	[Fact]
	public void TryResolve_WhenLethal_SetsGameOverStateAndRecord()
	{
		var handler = new GameOverDownedHandler(new NarrativeService());
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 3, Entrance = DirectionHelper.Origin };
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = new RoomCoord(2, 1);
		session.Combat = new CombatState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		var context = new PlayerDownedContext
		{
			Vitals = new VitalsDamageResult { HpBefore = 2, DamageRequested = 5, HypotheticalHpAfter = -3, HpAfterClamped = 0, OverkillMagnitude = 3 },
			DamageSource = new PlayerDamageSource { Type = DamageSourceType.Monster, DisplayName = "Rat King" }
		};

		Assert.True(handler.TryResolve(session, context));
		Assert.Equal(GamePlayPhase.GameOver, session.Phase);
		Assert.Null(session.Combat);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
		Assert.NotNull(session.LastFallenAdventurer);
		Assert.Equal(3, session.LastFallenAdventurer!.DeathFloorLevel);
		Assert.Equal(new RoomCoord(2, 1), session.LastFallenAdventurer.DeathRoomCoord);
	}
}
