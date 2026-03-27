using System;
using System.Collections.Generic;

public sealed class DungeonState
{
	private DungeonFloor? _currentFloor;
	private RoomCoord _playerCoord;

	public event Action? PlayerMoved;

	public DungeonMode DungeonMode { get; set; } = DungeonMode.Exploration;
	public List<DungeonFloor> Floors { get; set; } = new();

	public DungeonFloor? CurrentFloor
	{
		get => _currentFloor;
		set
		{
			if (ReferenceEquals(_currentFloor, value))
				return;
			_currentFloor = value;
			PlayerMoved?.Invoke();
		}
	}

	public RoomCoord PlayerCoord
	{
		get => _playerCoord;
		set
		{
			if (_playerCoord == value)
				return;
			_playerCoord = value;
			PlayerMoved?.Invoke();
		}
	}

	public Dictionary<int, HashSet<RoomCoord>> DiscoveredRoomsByFloor { get; set; } = new();

	public DungeonRoom? CurrentRoom =>
		CurrentFloor != null && CurrentFloor.Rooms.TryGetValue(PlayerCoord, out var room)
			? room
			: null;
}
