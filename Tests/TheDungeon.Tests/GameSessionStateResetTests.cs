using System;
using Xunit;

public sealed class GameSessionStateResetTests
{
	[Fact]
	public void ResetForNewRunPreservingFallenRecord_ResetsRunStateButKeepsFallenRecord()
	{
		var session = new GameSessionState();
		var fallen = new FallenAdventurerRecord
		{
			Character = PlayerCharacterSnapshot.From(new PlayerState { Name = "Ari" }),
			DeathFloorLevel = 2,
			DeathRoomCoord = new RoomCoord(2, 3),
			DamageSource = new PlayerDamageSource { Type = DamageSourceType.Trap, DisplayName = "Trap" },
			HpBeforeLethalBlow = 4,
			DamageRequested = 8,
			HypotheticalHpAfter = -4,
			OverkillMagnitude = 4,
			UtcTimestamp = DateTime.UtcNow,
		};

		session.Phase = GamePlayPhase.GameOver;
		session.GameOverTitle = "Game Over";
		session.GameOverBody = "Body";
		session.Combat = new CombatState();
		session.Player.Name = "Changed";
		session.Player.CurrentHp = 1;
		session.Player.MaxHp = 20;
		session.Player.Purse.Copper = 99;
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Dungeon.CurrentFloor = new DungeonFloor { Level = 9, Entrance = DirectionHelper.Origin };
		session.Dungeon.Floors.Add(session.Dungeon.CurrentFloor);
		session.Dungeon.DiscoveredRoomsByFloor[9] = [DirectionHelper.Origin];
		session.Dungeon.PlayerCoord = new RoomCoord(9, 9);
		session.LogEntries.Add(new LogEntry { Kind = LogEntryKind.Normal, Text = "line" });
		session.LastFallenAdventurer = fallen;

		session.ResetForNewRunPreservingFallenRecord();

		Assert.Equal(GamePlayPhase.InProgress, session.Phase);
		Assert.Null(session.GameOverTitle);
		Assert.Null(session.GameOverBody);
		Assert.Null(session.Combat);
		Assert.Equal("Testy McTestface", session.Player.Name);
		Assert.Equal(DungeonMode.Exploration, session.Dungeon.DungeonMode);
		Assert.Empty(session.Dungeon.Floors);
		Assert.Empty(session.Dungeon.DiscoveredRoomsByFloor);
		Assert.Equal(default, session.Dungeon.PlayerCoord);
		Assert.Empty(session.LogEntries);
		Assert.Equal(1, session.LogContentRevision);
		Assert.Same(fallen, session.LastFallenAdventurer);
	}
}
