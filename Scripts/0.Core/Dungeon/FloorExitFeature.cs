public sealed class FloorExitFeature : RoomFeature
{
	public FloorConnectionType ExitType { get; set; }

	/// <summary>
	/// When <see cref="ExitType"/> is <see cref="FloorConnectionType.Hole"/>: a rope has been tied off at this opening,
	/// so the hole can be climbed without spending another rope. Ignored for stairs/ladder.
	/// </summary>
	public bool RopeAnchored { get; set; }
}
