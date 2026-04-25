#nullable enable
using System.Collections.Generic;

/// <summary>Builds attack-style choices (unarmed + equipped weapons in hand slots) for combat UI.</summary>
public static class PlayerAttackOptionsResolver
{
	private static readonly EquipmentSlot[] HandWeaponSlots =
	{
		EquipmentSlot.WeaponMainHand1,
		EquipmentSlot.WeaponOffHand1,
		EquipmentSlot.WeaponMainHand2,
		EquipmentSlot.WeaponOffHand2,
	};

	public static IReadOnlyList<TargetDescriptor> ResolveWeaponChoiceDescriptors(InventoryState inventory)
	{
		var list = new List<TargetDescriptor>
		{
			new()
			{
				Label = "Unarmed",
				HighlightKey = null,
				Payload = new TargetPayload
				{
					Kind = TargetPayloadKind.PlayerAttackWeaponPick,
					AttackChoice = PlayerAttackChoice.Unarmed,
				},
			},
		};

		foreach (var slot in HandWeaponSlots)
		{
			if (!inventory.EquippedBySlot.TryGetValue(slot, out var inst) || inst == null)
				continue;
			if (inst.Definition is not WeaponDefinition w)
				continue;

			list.Add(new TargetDescriptor
			{
				Label = $"{w.Name} ({SlotShortLabel(slot)})",
				HighlightKey = null,
				Payload = new TargetPayload
				{
					Kind = TargetPayloadKind.PlayerAttackWeaponPick,
					AttackChoice = PlayerAttackChoice.Weapon(slot),
				},
			});
		}

		return list;
	}

	/// <summary>True when the player has at least one equipped weapon in a hand slot (so unarmed vs weapon is a real choice).</summary>
	public static bool HasWeaponChoiceBeyondUnarmed(InventoryState inventory) =>
		ResolveWeaponChoiceDescriptors(inventory).Count > 1;

	private static string SlotShortLabel(EquipmentSlot slot) =>
		slot switch
		{
			EquipmentSlot.WeaponMainHand1 => "main hand",
			EquipmentSlot.WeaponOffHand1 => "off hand",
			EquipmentSlot.WeaponMainHand2 => "main hand 2",
			EquipmentSlot.WeaponOffHand2 => "off hand 2",
			_ => slot.ToString(),
		};
}
