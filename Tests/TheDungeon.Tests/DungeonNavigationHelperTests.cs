using Xunit;

public sealed class DungeonNavigationHelperTests
{
	private static DungeonNavigationHelper.FloorTraversalContext Ctx(bool hasRope) =>
		new() { HasRope = hasRope };

	[Fact]
	public void Hole_BlockWhenUnanchoredNoRope_NoPaired()
	{
		var exit = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = false };
		Assert.False(DungeonNavigationHelper.IsVerticalConnectionTraversable(exit, Ctx(hasRope: false)));
	}

	[Fact]
	public void Hole_AllowWhenHasRope_Unanchored()
	{
		var exit = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = false };
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(exit, Ctx(hasRope: true)));
	}

	[Fact]
	public void Hole_AllowWhenAnchored_NoRope()
	{
		var exit = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = true };
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(exit, Ctx(hasRope: false)));
	}

	[Fact]
	public void Hole_PairedOppositeAnchored_AllowsWithoutRope()
	{
		var upper = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = false };
		var lower = new FloorExitFeature { ExitType = FloorConnectionType.Hole, RopeAnchored = true };
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(upper, lower, Ctx(hasRope: false)));
	}

	[Fact]
	public void GetNeighborCoord_MovesByDirection()
	{
		var origin = new RoomCoord(5, 5);

		Assert.Equal(new RoomCoord(5, 4), DungeonNavigationHelper.GetNeighborCoord(origin, HorizontalDirection.North));
		Assert.Equal(new RoomCoord(6, 5), DungeonNavigationHelper.GetNeighborCoord(origin, HorizontalDirection.East));
		Assert.Equal(new RoomCoord(5, 6), DungeonNavigationHelper.GetNeighborCoord(origin, HorizontalDirection.South));
		Assert.Equal(new RoomCoord(4, 5), DungeonNavigationHelper.GetNeighborCoord(origin, HorizontalDirection.West));
	}

	[Fact]
	public void IsConnectionTraversable_RespectsContextFlags()
	{
		var blockedDoor = new DungeonNavigationHelper.RoomTraversalContext
		{
			DoorIsUnblocked = false,
			SecretOrIllusoryRevealed = false,
		};
		var revealed = new DungeonNavigationHelper.RoomTraversalContext
		{
			DoorIsUnblocked = true,
			SecretOrIllusoryRevealed = true,
		};

		Assert.False(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.None, blockedDoor));
		Assert.True(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.Passage, blockedDoor));
		Assert.False(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.Door, blockedDoor));
		Assert.True(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.Door, revealed));
		Assert.False(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.SecretDoor, blockedDoor));
		Assert.True(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.SecretDoor, revealed));
		Assert.False(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.IllusoryWall, blockedDoor));
		Assert.True(DungeonNavigationHelper.IsConnectionTraversable(RoomConnectionType.IllusoryWall, revealed));
	}

	[Fact]
	public void IsVerticalConnectionTraversable_ByConnectionType()
	{
		Assert.False(DungeonNavigationHelper.IsVerticalConnectionTraversable(FloorConnectionType.None, Ctx(hasRope: false)));
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(FloorConnectionType.Stairs, Ctx(hasRope: false)));
		Assert.True(DungeonNavigationHelper.IsVerticalConnectionTraversable(FloorConnectionType.Ladder, Ctx(hasRope: false)));
		Assert.False(DungeonNavigationHelper.IsVerticalConnectionTraversable(FloorConnectionType.Hole, Ctx(hasRope: false)));
	}
}
