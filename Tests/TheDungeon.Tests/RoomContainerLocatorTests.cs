using Xunit;

public sealed class RoomContainerLocatorTests
{
	private static GameSessionState SessionInRoom(DungeonRoom room)
	{
		var s = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = room;
		s.Dungeon.CurrentFloor = floor;
		s.Dungeon.PlayerCoord = DirectionHelper.Origin;
		return s;
	}

	[Fact]
	public void CurrentRoomHasAnyContainer_True_WhenSalvagePresent()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new SalvageFeature { Contents = [] });
		var session = SessionInRoom(room);

		Assert.True(RoomContainerLocator.CurrentRoomHasAnyContainer(session));
	}

	[Fact]
	public void CurrentRoomHasAnyContainer_False_WhenNoContainerFeature()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new TreasureFeature
		{
			TreasureItems = [new TreasureInstance { IsRevealed = true, Definition = new TreasureDefinition { Id = "g", Name = "G", GrantKind = TreasureKind.Gold, ValueInGp = 1 } }],
		});
		var session = SessionInRoom(room);

		Assert.False(RoomContainerLocator.CurrentRoomHasAnyContainer(session));
	}

	[Fact]
	public void CurrentRoomHasAnyContainer_False_WhenNoCurrentRoom()
	{
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		floor.Rooms[DirectionHelper.Origin] = new DungeonRoom { Position = DirectionHelper.Origin };
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = new RoomCoord(99, 99);

		Assert.False(RoomContainerLocator.CurrentRoomHasAnyContainer(session));
	}

	[Fact]
	public void TryGetNthContainer_MatchesOpenContainerResolverOrdinals()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		var s0 = new SalvageFeature { Contents = [] };
		room.Features.Add(s0);
		room.Features.Add(new TreasureFeature
		{
			TreasureItems =
			[
				new TreasureInstance
				{
					IsRevealed = true,
					Definition = new TreasureDefinition { Id = "g", Name = "G", GrantKind = TreasureKind.Gold, ValueInGp = 1 },
				},
			],
		});
		var c1 = new ChestFeature { Locked = false, Contents = [] };
		room.Features.Add(c1);

		Assert.True(RoomContainerLocator.TryGetNthContainer(room, 0, out var a) && ReferenceEquals(a, s0));
		Assert.True(RoomContainerLocator.TryGetNthContainer(room, 1, out var b) && ReferenceEquals(b, c1));
		Assert.False(RoomContainerLocator.TryGetNthContainer(room, 2, out _));
	}

	[Fact]
	public void TryGetNthContainer_SetsWasOpened_SameOrdinalAsOpenTargets()
	{
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new SalvageFeature { Contents = [] });
		var session = SessionInRoom(room);
		var open = PlayerActionTargetResolvers.ResolveOpenContainerTargets(session, DungeonMode.Exploration);
		Assert.Single(open);
		Assert.Equal(0, open[0].Payload.ContainerOrdinal);

		Assert.True(RoomContainerLocator.TryGetNthContainer(room, 0, out var cf) && cf != null);
		cf.WasOpened = true;
		Assert.Contains("salvage_opened", PresentationIconKeys.MainView.ForContainer(cf), System.StringComparison.Ordinal);
	}
}
