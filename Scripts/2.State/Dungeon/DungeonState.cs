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

	/// <summary>Absolute side of the current cell where the player entered from the previous horizontal move; leaving through this facing is backtracking.</summary>
	public HorizontalDirection? EnteredFromCompass { get; set; }

	/// <summary>Set when the player entered this cell via a vertical connection (used to exempt reversal moves).</summary>
	public VerticalIngressKind VerticalIngress { get; set; }

	public void ClearRoomIngress()
	{
		EnteredFromCompass = null;
		VerticalIngress = VerticalIngressKind.None;
	}

	/// <summary>Call after a successful horizontal step into a room: came from behind you relative to the direction you walked.</summary>
	public void SetIngressAfterHorizontalEnter(HorizontalDirection facingUsedToLeavePreviousRoom)
	{
		EnteredFromCompass = DirectionHelper.TurnAround(facingUsedToLeavePreviousRoom);
		VerticalIngress = VerticalIngressKind.None;
	}

	public DungeonRoom? CurrentRoom =>
		CurrentFloor != null && CurrentFloor.Rooms.TryGetValue(PlayerCoord, out var room)
			? room
			: null;
}
