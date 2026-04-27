using System.Collections.Generic;
using Xunit;

public sealed class MainViewPresentationBuilderTests
{
	[Fact]
	public void Build_WithCurrentRoom_ComputesFacingRelativeConnectionsAndTitle()
	{
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 2, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = new RoomCoord(1, 2) };
		room.Exits.Set(HorizontalDirection.North, RoomConnectionType.Passage);
		room.Exits.Set(HorizontalDirection.East, RoomConnectionType.Door);
		room.Exits.Set(HorizontalDirection.South, RoomConnectionType.SecretDoor);
		room.Exits.Set(HorizontalDirection.West, RoomConnectionType.None);
		floor.Rooms[room.Position] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = room.Position;
		session.Player.Facing = HorizontalDirection.East;

		var model = MainViewPresentationBuilder.Build(session, FloorConnectionType.Stairs);

		Assert.Contains("Floor 2", model.Title);
		Assert.Contains("Room (1, 2)", model.Title);
		Assert.Contains("Facing: East", model.Title);
		Assert.Contains("Stairs", model.Title);
		Assert.Equal(RoomConnectionType.Passage, model.LeftConnection);  // north
		Assert.Equal(RoomConnectionType.Door, model.FrontConnection);    // east
		Assert.Equal(RoomConnectionType.SecretDoor, model.RightConnection); // south
	}

	[Fact]
	public void Build_WhenNoCurrentRoom_UsesNoneConnectionsAndEmptySlots()
	{
		var session = new GameSessionState();
		session.Player.Facing = HorizontalDirection.North;
		session.Dungeon.CurrentFloor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		session.Dungeon.PlayerCoord = DirectionHelper.Origin; // no room added

		var model = MainViewPresentationBuilder.Build(session, FloorConnectionType.None);

		Assert.Equal(RoomConnectionType.None, model.LeftConnection);
		Assert.Equal(RoomConnectionType.None, model.FrontConnection);
		Assert.Equal(RoomConnectionType.None, model.RightConnection);
		Assert.Empty(model.FeatureSlots);
	}

	[Fact]
	public void Build_AppliesTargetingLabelsByHighlightKey()
	{
		var session = new GameSessionState();
		var floor = new DungeonFloor { Level = 1, Entrance = DirectionHelper.Origin };
		var room = new DungeonRoom { Position = DirectionHelper.Origin };
		room.Features.Add(new MonsterFeature
		{
			Monsters =
			[
				new MonsterInstance
				{
					CurrentHp = 5,
					Definition = new MonsterDefinition { Id = "rat", Name = "Rat" }
				}
			]
		});
		floor.Rooms[DirectionHelper.Origin] = room;
		session.Dungeon.CurrentFloor = floor;
		session.Dungeon.PlayerCoord = DirectionHelper.Origin;

		var labels = new Dictionary<string, string> { ["monster:0"] = "Attack Rat" };
		var model = MainViewPresentationBuilder.Build(session, FloorConnectionType.None, labels);

		Assert.Single(model.FeatureSlots);
		Assert.Equal("monster:0", model.FeatureSlots[0].HighlightKey);
		Assert.Equal("Attack Rat", model.FeatureSlots[0].TargetingLabel);
	}
}
