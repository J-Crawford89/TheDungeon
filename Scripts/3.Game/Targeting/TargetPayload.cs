#nullable enable

public enum TargetPayloadKind
{
	AttackLivingMonsterOrdinal,
	TakeTreasureItem,
	TakeAllEligibleTreasure,
	LootContainerAll,
	DisarmTrapInstance,
	PlayerAttackWeaponPick,
}

/// <summary>How to execute a player-chosen target (no behavior on this type).</summary>
public sealed class TargetPayload
{
	public required TargetPayloadKind Kind { get; init; }

	/// <summary>Ordinal of living monsters in <see cref="MainViewRoomSlots"/> order (attack).</summary>
	public int LivingMonsterOrdinal { get; init; }

	public int TreasureFeatureOrdinal { get; init; }
	public int TreasureItemIndexInFeature { get; init; }

	public int TrapFeatureOrdinal { get; init; }
	public int TrapIndexInFeature { get; init; }

	/// <summary>Ordinal among <see cref="ContainerFeature"/> instances in <see cref="DungeonRoom.Features"/> order (matches <see cref="RoomContainerLocator"/>).</summary>
	public int ContainerOrdinal { get; init; }

	/// <summary>When <see cref="Kind"/> is <see cref="TargetPayloadKind.PlayerAttackWeaponPick"/>.</summary>
	public PlayerAttackChoice AttackChoice { get; init; }
}
