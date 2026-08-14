using Xunit;

public sealed class InitiativeTurnOrderViewTests
{
	[Fact]
	public void FromSession_WhenNotInCombat_ReturnsEmpty()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Exploration;
		session.Combat = new CombatState
		{
			TurnOrder = [new CombatTurnSlot { IsPlayer = true }],
			CurrentTurnIndex = 0,
		};

		var view = InitiativeTurnOrderView.FromSession(session);

		Assert.Empty(view.Names);
		Assert.Equal(string.Empty, view.OverlayText);
	}

	[Fact]
	public void FromSession_WhenCombatHasNoTurnOrder_ReturnsEmpty()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState { TurnOrder = [], CurrentTurnIndex = 0 };

		var view = InitiativeTurnOrderView.FromSession(session);

		Assert.Empty(view.Names);
	}

	[Fact]
	public void FromSession_WhenCombatNull_ReturnsEmpty()
	{
		var session = new GameSessionState();
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = null;

		var view = InitiativeTurnOrderView.FromSession(session);

		Assert.Empty(view.Names);
	}

	[Fact]
	public void FromSession_MapsPlayerAndMonsterNamesAndCurrentIndex()
	{
		var session = SessionWithRatCombat(currentTurnIndex: 1);

		var view = InitiativeTurnOrderView.FromSession(session);

		Assert.Equal(new[] { "You", "Rat" }, view.Names);
		Assert.Equal(1, view.CurrentIndex);
		Assert.Equal("You → Rat", view.OverlayText);
	}

	[Fact]
	public void FromSession_ClampsInvalidCurrentIndex()
	{
		var session = SessionWithRatCombat(currentTurnIndex: 99);

		var view = InitiativeTurnOrderView.FromSession(session);

		Assert.Equal(0, view.CurrentIndex);
	}

	private static GameSessionState SessionWithRatCombat(int currentTurnIndex)
	{
		var monster = new MonsterInstance
		{
			CurrentHp = 4,
			Definition = new MonsterDefinition { Id = "rat", Name = "Rat" },
		};
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature { Monsters = [monster] });
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;

		var session = new GameSessionState();
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;
		session.Dungeon.DungeonMode = DungeonMode.Combat;
		session.Combat = new CombatState
		{
			TurnOrder =
			[
				new CombatTurnSlot { IsPlayer = true },
				new CombatTurnSlot { IsPlayer = false, MonsterIndex = 0 },
			],
			CurrentTurnIndex = currentTurnIndex,
		};
		return session;
	}
}
