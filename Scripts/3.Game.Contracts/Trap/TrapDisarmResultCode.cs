/// <summary>
/// Preconditions for disarm or <see cref="DisarmCheckResolved"/> when the dexterity vs DC roll was performed.
/// </summary>
public enum TrapDisarmResultCode
{
	NoCurrentFloor,
	NoCurrentRoom,
	NoTrapPresent,
	DisarmCheckResolved,
}
