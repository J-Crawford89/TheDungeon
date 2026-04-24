using System.Collections.Generic;

public static class MainViewPresentationBuilder
{
	public static MainViewRenderModel Build(
		GameSessionState session,
		FloorConnectionType verticalConnection,
		IReadOnlyDictionary<string, string>? targetingLabelsByHighlightKey = null)
	{
		var dungeon = session.Dungeon;
		var facing = session.Player.Facing;
		var coord = dungeon.PlayerCoord;

		var verticalNote = verticalConnection == FloorConnectionType.None
			? string.Empty
			: $" | {verticalConnection}";
		var title = $"Room {coord} — Facing: {facing}{verticalNote}";

		RoomConnectionType left, front, right;
		if (dungeon.CurrentRoom is { } room)
		{
			left = room.Exits.Get(DirectionHelper.TurnLeft(facing));
			front = room.Exits.Get(facing);
			right = room.Exits.Get(DirectionHelper.TurnRight(facing));
		}
		else
			left = front = right = RoomConnectionType.None;

		var rawSlots = dungeon.CurrentRoom is { } r
			? MainViewRoomSlots.Enumerate(r)
			: (IReadOnlyList<MainViewFeatureSlot>)[];

		var slots = new List<MainViewFeatureSlot>(rawSlots.Count);
		foreach (var s in rawSlots)
		{
			string? label = null;
			if (targetingLabelsByHighlightKey != null &&
			    targetingLabelsByHighlightKey.TryGetValue(s.HighlightKey, out var l))
				label = l;
			slots.Add(new MainViewFeatureSlot
			{
				HighlightKey = s.HighlightKey,
				PresentationIconKey = s.PresentationIconKey,
				TargetingLabel = label
			});
		}

		return new MainViewRenderModel
		{
			Title = title,
			LeftConnection = left,
			FrontConnection = front,
			RightConnection = right,
			FeatureSlots = slots
		};
	}
}
