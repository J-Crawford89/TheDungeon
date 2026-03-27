using Godot;
using System.Collections.Generic;

public partial class MapPanel : PanelContainer
{
	[Export] private MapView _mapView;

	public void RefreshMap(GameSessionState session)
	{
		var floor = session.Dungeon.CurrentFloor;
		if (floor == null)
			return;
		if (!session.Dungeon.DiscoveredRoomsByFloor.TryGetValue(floor.Level, out var discovered))
			discovered = new HashSet<RoomCoord>();
		_mapView.SetMapData(floor, discovered, session.Dungeon.PlayerCoord);
	}
}
