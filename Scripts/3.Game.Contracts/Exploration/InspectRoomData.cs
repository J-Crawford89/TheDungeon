using System.Collections.Generic;

public sealed class InspectRoomData
{
	public List<(HorizontalDirection Side, RoomConnectionType Connection)> Exits { get; set; } = new();

	public FloorConnectionType VerticalConnection { get; set; }

	/// <summary>When <see cref="VerticalConnection"/> is Hole: whether a rope is tied at this opening.</summary>
	public bool HoleRopeAnchored { get; set; }

	public List<InspectRoomFeatureLine> FeatureLines { get; set; } = new();
}
