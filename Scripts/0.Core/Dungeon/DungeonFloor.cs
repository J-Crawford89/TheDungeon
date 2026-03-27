using System.Collections.Generic;

public sealed class DungeonFloor
{
    public int Level { get; set; }
    public Dictionary<RoomCoord, DungeonRoom> Rooms { get; } = new();
	public RoomCoord Entrance { get; set; }
}
