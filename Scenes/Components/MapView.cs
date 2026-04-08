#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class MapView : Control
{
	[Export] public int CellSize { get; set; } = 22;
	[Export] public int RoomSize { get; set; } = 14;
	[Export] public int LineWidth { get; set; } = 2;
	[Export] public int Padding { get; set; } = 14;
	[Export] public int CurrentRoomInset { get; set; } = 6;

	private DungeonFloor? _floor;
	private HashSet<RoomCoord> _discoveredRooms = new();
	private RoomCoord? _currentRoom;

	public void SetMapData(DungeonFloor floor, IEnumerable<RoomCoord> discoveredRooms, RoomCoord currentRoom)
	{
		_floor = floor;
		_discoveredRooms = discoveredRooms.ToHashSet();
		_currentRoom = currentRoom;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_floor is null || _discoveredRooms.Count == 0) return;

		foreach (var room in _floor.Rooms.Values)
		{
			if (!_discoveredRooms.Contains(room.Position)) continue;

			var roomCenter = GetRoomCenter(room.Position);
			var roomRect = new Rect2(
				roomCenter - new Vector2(RoomSize / 2f, RoomSize / 2f),
				new Vector2(RoomSize, RoomSize));

			DrawRect(roomRect, Colors.White, filled: false, width: LineWidth);

			DrawExit(room.Position, DungeonNavigationHelper.IsConnectionTraversable(room.Exits.Get(HorizontalDirection.North)), new Vector2(0, -1));
			DrawExit(room.Position, DungeonNavigationHelper.IsConnectionTraversable(room.Exits.Get(HorizontalDirection.South)), new Vector2(0, 1));
			DrawExit(room.Position, DungeonNavigationHelper.IsConnectionTraversable(room.Exits.Get(HorizontalDirection.East)), new Vector2(1, 0));
			DrawExit(room.Position, DungeonNavigationHelper.IsConnectionTraversable(room.Exits.Get(HorizontalDirection.West)), new Vector2(-1, 0));

			if (room.Features.OfType<FloorExitFeature>().Any())
			{
				var markerRect = new Rect2(
					roomCenter - new Vector2(2, 2),
					new Vector2(4, 4));

				DrawRect(markerRect, Colors.White, filled: true);
			}
		}

		if (_currentRoom.HasValue)
		{
			var currentCenter = GetRoomCenter(_currentRoom.Value);
			var currentRect = new Rect2(
				currentCenter - new Vector2((RoomSize - CurrentRoomInset) / 2f, (RoomSize - CurrentRoomInset) / 2f),
				new Vector2(RoomSize - CurrentRoomInset, RoomSize - CurrentRoomInset));

			DrawRect(currentRect, Colors.Red, filled: false, width: LineWidth);
		}
	}

	private void DrawExit(RoomCoord roomCoord, bool hasExit, Vector2 direction)
	{
		if (!hasExit) return;

		var roomCenter = GetRoomCenter(roomCoord);
		var halfRoom = RoomSize / 2f;
		var start = roomCenter + direction * halfRoom;
		var end = roomCenter + direction * (CellSize / 2f);

		DrawLine(start, end, Colors.White, LineWidth);
	}

	private Vector2 GetRoomCenter(RoomCoord coord)
	{
		var minX = _discoveredRooms.Min(r => r.X);
		var minY = _discoveredRooms.Min(r => r.Y);

		return new Vector2(
			Padding + (coord.X - minX) * CellSize,
			Padding + (coord.Y - minY) * CellSize);
	}
}
