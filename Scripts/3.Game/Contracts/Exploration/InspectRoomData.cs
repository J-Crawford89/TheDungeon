using System.Collections.Generic;

public sealed class InspectRoomData
{
	public List<(HorizontalDirection Side, RoomConnectionType Connection)> Exits { get; set; } = new();

	public FloorConnectionType VerticalConnection { get; set; }

	public List<InspectRoomFeatureLine> FeatureLines { get; set; } = new();
}
