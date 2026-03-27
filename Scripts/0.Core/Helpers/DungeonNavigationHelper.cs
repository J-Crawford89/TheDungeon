public static class DungeonNavigationHelper
{
	public readonly struct RoomTraversalContext
	{
		public bool SecretOrIllusoryRevealed { get; init; }
		public bool DoorIsUnblocked { get; init; }

		public static RoomTraversalContext StandardMovement => new()
		{
			SecretOrIllusoryRevealed = false,
			DoorIsUnblocked = true
		};
	}

	public readonly struct FloorTraversalContext
	{
		public bool HasRope { get; init; }

		public static FloorTraversalContext StandardMovement => new()
		{
			HasRope = false,
		};
	}

	public static RoomCoord GetNeighborCoord(RoomCoord origin, HorizontalDirection direction) =>
		direction switch
		{
			HorizontalDirection.North => new RoomCoord(origin.X, origin.Y - 1),
			HorizontalDirection.East => new RoomCoord(origin.X + 1, origin.Y),
			HorizontalDirection.South => new RoomCoord(origin.X, origin.Y + 1),
			HorizontalDirection.West => new RoomCoord(origin.X - 1, origin.Y),
			_ => origin
		};

	public static bool IsConnectionTraversable(RoomConnectionType connection) =>
		IsConnectionTraversable(connection, RoomTraversalContext.StandardMovement);

	public static bool IsConnectionTraversable(RoomConnectionType connection, in RoomTraversalContext context) =>
		connection switch
		{
			RoomConnectionType.None => false,
			RoomConnectionType.Passage => true,
			RoomConnectionType.Door => context.DoorIsUnblocked,
			RoomConnectionType.SecretDoor => context.SecretOrIllusoryRevealed,
			RoomConnectionType.IllusoryWall => context.SecretOrIllusoryRevealed,
			_ => false
		};

	public static bool IsVerticalConnectionTraversable(FloorConnectionType connection) =>
		IsVerticalConnectionTraversable(connection, FloorTraversalContext.StandardMovement);

	public static bool IsVerticalConnectionTraversable(FloorConnectionType connection, in FloorTraversalContext context) =>
		connection switch
		{
			FloorConnectionType.None => false,
			FloorConnectionType.Stairs => true,
			FloorConnectionType.Ladder => true,
			FloorConnectionType.Hole => context.HasRope,
			_ => false
		};
}
