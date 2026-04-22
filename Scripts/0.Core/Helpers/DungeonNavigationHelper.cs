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

	/// <summary>
	/// Vertical traversal using only connection kind (no per-room rope state). Holes require rope in context.
	/// </summary>
	public static bool IsVerticalConnectionTraversable(FloorConnectionType connection, in FloorTraversalContext context) =>
		connection switch
		{
			FloorConnectionType.None => false,
			FloorConnectionType.Stairs => true,
			FloorConnectionType.Ladder => true,
			FloorConnectionType.Hole => context.HasRope,
			_ => false
		};

	/// <summary>
	/// Uses <see cref="FloorExitFeature.RopeAnchored"/> for holes when only one side is known (e.g. generating the floor below).
	/// </summary>
	public static bool IsVerticalConnectionTraversable(FloorExitFeature exit, in FloorTraversalContext context) =>
		IsVerticalConnectionTraversable(exit, pairedOppositeExit: null, context);

	/// <summary>
	/// Full hole rule: passable if either opening has an anchored rope, or the player carries rope.
	/// </summary>
	public static bool IsVerticalConnectionTraversable(
		FloorExitFeature exit,
		FloorExitFeature? pairedOppositeExit,
		in FloorTraversalContext context) =>
		exit.ExitType switch
		{
			FloorConnectionType.None => false,
			FloorConnectionType.Stairs => true,
			FloorConnectionType.Ladder => true,
			FloorConnectionType.Hole =>
				exit.RopeAnchored ||
				pairedOppositeExit?.RopeAnchored == true ||
				context.HasRope,
			_ => false
		};
}
