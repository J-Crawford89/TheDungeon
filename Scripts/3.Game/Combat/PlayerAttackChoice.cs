#nullable enable

/// <summary>How the player resolves a melee attack (unarmed vs a specific equipped weapon slot).</summary>
public readonly struct PlayerAttackChoice
{
	/// <summary>When null, attack is unarmed. Otherwise one of the weapon equipment slots.</summary>
	public EquipmentSlot? WeaponSlotIfAny { get; init; }

	public bool IsUnarmed => WeaponSlotIfAny == null;

	public static PlayerAttackChoice Unarmed => default;

	public static PlayerAttackChoice Weapon(EquipmentSlot slot) => new() { WeaponSlotIfAny = slot };
}
