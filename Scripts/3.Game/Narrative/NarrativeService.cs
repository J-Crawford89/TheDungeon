using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed partial class NarrativeService : INarrativeTextProvider
{
	public string ForEnterDungeon() => "You enter the dungeon.";

	public string ForMoveForward(ExplorationServiceResult result)
	{
		if (result.Success && result.DestinationAfterMove is { } destination)
			return $"You move forward into room {destination}.";

		return result.ErrorCode switch
		{
			ExplorationErrorCode.NoCurrentFloor => "There is nowhere to go.",
			ExplorationErrorCode.NoRoomAtPlayer => "You are nowhere.",
			ExplorationErrorCode.MoveBlocked => "You cannot go that way.",
			ExplorationErrorCode.NoRoomAtTargetCoord => "You cannot go that way.",
			_ => "You cannot go that way."
		};
	}

	public string? ForTurn(ExplorationServiceResult result, DirectionTurned turn)
	{
		if (!result.Success)
			return null;

		if (result.FacingAfterRotation is { } facing)
			return $"You turn {turn.ToString().ToLower()}. You now face {facing}.";

		return null;
	}

	public string ForAboutFace(ExplorationServiceResult result)
	{
		if (result.Success && result.FacingAfterRotation is { } facing)
			return $"You turn around. You now face {facing}.";

		return string.Empty;
	}

	public string ForInspect(ExplorationServiceResult result)
	{
		if (result.Success && result.InspectData is { } data)
			return FormatInspectRoom(data);

		return result.ErrorCode switch
		{
			ExplorationErrorCode.InspectNoFloor or ExplorationErrorCode.InspectNoRoom => "There is nothing to see.",
			_ => "There is nothing to see."
		};
	}

	public string ForMoveUpFloor(ExplorationServiceResult result)
	{
		if (result.Success && result.DestinationAfterMove is { } destination && result.FloorAfterMove is { } floor)
		{
			var msg = $"You ascend to level {floor}. You are in room {destination}.";
			if (result.ConsumedRopeForHole)
				return msg + " You tie off your rope and climb up.";
			if (result.HoleWasAlreadyAnchored)
				return msg + " You climb the anchored rope.";
			return msg;
		}

		return result.ErrorCode switch
		{
			_ => "You cannot go that way."
		};
	}

	public string ForMoveDownFloor(ExplorationServiceResult result)
	{
		if (result.Success && result.DestinationAfterMove is { } destination && result.FloorAfterMove is { } floor)
		{
			var msg = $"You descend to level {floor}. You are in room {destination}.";
			if (result.ConsumedRopeForHole)
				return msg + " You tie off the rope and rappel down.";
			if (result.HoleWasAlreadyAnchored)
				return msg + " You descend using the rope.";
			return msg;
		}

		return result.ErrorCode switch
		{
			_ => "You cannot go that way."
		};
	}

	private static string FormatInspectRoom(InspectRoomData data)
	{
		var description = new StringBuilder();
		description.Append("Exits: ");
		var exitDescriptions = new List<string>();
		foreach (var (side, connection) in data.Exits)
			exitDescriptions.Add($"{side}: {DescribeConnection(connection)}");

		if (exitDescriptions.Count == 0)
			description.Append("none obvious.");
		else
			description.Append(string.Join(", ", exitDescriptions));

		return description.ToString();
	}

	/// <summary>
	/// Vertical connection and room features are logged separately as <see cref="LogEntryKind.Important"/>; not inlined into the main inspect line.
	/// </summary>
	public IReadOnlyList<string> GetInspectImportantLogLines(InspectRoomData data)
	{
		var lines = new List<string>();
		if (data.VerticalConnection != FloorConnectionType.None)
		{
			var vertical = DescribeVertical(data.VerticalConnection, data.HoleRopeAnchored);
			if (!string.IsNullOrEmpty(vertical))
				lines.Add(vertical + ".");
		}

		foreach (var line in data.FeatureLines)
		{
			if (!string.IsNullOrEmpty(line.Text))
				lines.Add(line.Text);
		}

		return lines;
	}

	private static string DescribeConnection(RoomConnectionType connectionType) =>
		connectionType switch
		{
			RoomConnectionType.Passage => "open passage",
			RoomConnectionType.Door => "a door",
			RoomConnectionType.SecretDoor => "a solid wall",
			RoomConnectionType.IllusoryWall => "a solid wall",
			_ => "blocked"
		};

	private static string DescribeVertical(FloorConnectionType verticalKind, bool holeRopeAnchored = false) =>
		verticalKind switch
		{
			FloorConnectionType.Stairs => "Stairs lead down",
			FloorConnectionType.Ladder => "A ladder leads down",
			FloorConnectionType.Hole when holeRopeAnchored => "A rope hangs into a hole in the floor",
			FloorConnectionType.Hole => "A hole opens in the floor",
			_ => ""
		};
}
