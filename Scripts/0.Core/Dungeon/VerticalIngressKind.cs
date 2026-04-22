/// <summary>How the player entered the current room via a vertical connection (for backtracking exemption).</summary>
public enum VerticalIngressKind
{
	None = 0,
	/// <summary>Descended into this cell from the floor above (hole, stairs down).</summary>
	EnteredFromFloorAbove,
	/// <summary>Climbed into this cell from the floor below (stairs up, ladder).</summary>
	EnteredFromFloorBelow,
}
