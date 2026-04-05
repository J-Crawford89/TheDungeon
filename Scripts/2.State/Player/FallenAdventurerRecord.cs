using System;

public sealed class FallenAdventurerRecord
{
	public required PlayerCharacterSnapshot Character { get; init; }
	public int DeathFloorLevel { get; init; }
	public required RoomCoord DeathRoomCoord { get; init; }
	public required PlayerDamageSource DamageSource { get; init; }
	public int HpBeforeLethalBlow { get; init; }
	public int DamageRequested { get; init; }
	public int HypotheticalHpAfter { get; init; }
	public int OverkillMagnitude { get; init; }
	public DateTime UtcTimestamp { get; init; }
}
