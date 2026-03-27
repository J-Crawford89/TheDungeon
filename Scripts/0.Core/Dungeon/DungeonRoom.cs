using System.Collections.Generic;

public sealed class DungeonRoom
{
	public RoomCoord Position { get; set; }
	public RoomExits Exits { get; set; } = new RoomExits();
	public List<RoomFeature> Features { get; set; } = new();
}
