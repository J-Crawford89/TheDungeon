#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class ItemMapper
{
	public static ItemDefinition ToDomain(ItemResource resource) =>
		resource switch
		{
			WeaponResource w => ToWeaponDefinition(w),
			ArmorResource a => ToArmorDefinition(a),
			EquipmentResource e => ToEquipmentDefinition(e),
			PotionResource p => ToPotionDefinition(p),
			_ => ToBaseItemDefinition(resource)
		};

	private static ItemDefinition ToBaseItemDefinition(ItemResource resource) =>
		new()
		{
			Id = resource.Id,
			Name = resource.Name,
			Description = resource.Description,
			Rarity = resource.Rarity,
			ValueInCopper = resource.ValueInCopper,
			MaxStackSize = resource.MaxStackSize,
			CanDrop = resource.CanDrop,
			CanSell = resource.CanSell,
			Effects = MapItemEffects(resource.Effects),
		};

	private static List<ItemEffectDefinition> MapItemEffects(Array<ItemEffectResource>? resources)
	{
		var list = new List<ItemEffectDefinition>();
		if (resources == null)
			return list;

		foreach (var resource in resources)
		{
			if (resource == null)
				continue;
			switch (resource)
			{
				case RestoreHealthEffectResource restore:
					list.Add(ToRestoreHealthEffectDefinition(restore));
					break;
				case DamageReductionEffectResource reduction:
					list.Add(ToDamageReductionEffectDefinition(reduction));
					break;
				default:
					GD.PushWarning($"ItemMapper: unsupported item effect resource '{resource.GetType().Name}' on item effects list.");
					break;
			}
		}

		return list;
	}

	private static List<EquipmentSlot> MapEquipmentSlots(Array<EquipmentSlot>? slots)
	{
		var list = new List<EquipmentSlot>();
		if (slots == null)
			return list;
		for (var i = 0; i < slots.Count; i++)
			list.Add(slots[i]);
		return list;
	}

	private static EquipmentDefinition ToEquipmentDefinition(EquipmentResource resource)
	{
		var b = ToBaseItemDefinition(resource);
		return new EquipmentDefinition
		{
			Id = b.Id,
			Name = b.Name,
			Description = b.Description,
			Rarity = b.Rarity,
			ValueInCopper = b.ValueInCopper,
			MaxStackSize = b.MaxStackSize,
			CanDrop = b.CanDrop,
			CanSell = b.CanSell,
			Effects = new List<ItemEffectDefinition>(b.Effects),
			Slots = MapEquipmentSlots(resource.Slots),
			OccupiedSlots = MapEquipmentSlots(resource.OccupiedSlots),
		};
	}

	private static ArmorDefinition ToArmorDefinition(ArmorResource resource)
	{
		var eq = ToEquipmentDefinition(resource);
		return new ArmorDefinition
		{
			Id = eq.Id,
			Name = eq.Name,
			Description = eq.Description,
			Rarity = eq.Rarity,
			ValueInCopper = eq.ValueInCopper,
			MaxStackSize = eq.MaxStackSize,
			CanDrop = eq.CanDrop,
			CanSell = eq.CanSell,
			Effects = new List<ItemEffectDefinition>(eq.Effects),
			Slots = eq.Slots,
			OccupiedSlots = eq.OccupiedSlots,
			ArmorBonus = resource.ArmorBonus,
			AgilityPenalty = resource.AgilityPenalty,
		};
	}

	private static WeaponDefinition ToWeaponDefinition(WeaponResource resource)
	{
		var eq = ToEquipmentDefinition(resource);
		var attacks = AttackResourceMapper.ToDomainList(resource.Attacks);
		if (attacks.Count == 0)
			GD.PushWarning($"ItemMapper: weapon '{resource.Id}' has no valid attacks; assign AttackResource entries in the editor.");

		return new WeaponDefinition
		{
			Id = eq.Id,
			Name = eq.Name,
			Description = eq.Description,
			Rarity = eq.Rarity,
			ValueInCopper = eq.ValueInCopper,
			MaxStackSize = eq.MaxStackSize,
			CanDrop = eq.CanDrop,
			CanSell = eq.CanSell,
			Effects = new List<ItemEffectDefinition>(eq.Effects),
			Slots = eq.Slots,
			OccupiedSlots = eq.OccupiedSlots,
			Attacks = attacks,
			Category = MapWeaponCategory(resource.Category),
			SubCategory = MapWeaponSubCategory(resource.SubCategory),
			Group = MapWeaponGroup(resource.Group),
			SubGroup = MapWeaponSubGroup(resource.SubGroup),
			OwnershipProficiencyRank = resource.OwnershipProficiencyRank,
		};
	}

	private static WeaponCategoryDefinition MapWeaponCategory(WeaponCategoryResource? r)
	{
		if (r == null)
			return new WeaponCategoryDefinition();
		return new WeaponCategoryDefinition
		{
			Id = r.Id?.Trim() ?? "",
			Name = r.Name?.Trim() ?? "",
			Description = r.Description?.Trim() ?? "",
		};
	}

	private static WeaponSubCategoryDefinition MapWeaponSubCategory(WeaponSubCategoryResource? r)
	{
		if (r == null)
			return new WeaponSubCategoryDefinition();
		return new WeaponSubCategoryDefinition
		{
			Id = r.Id?.Trim() ?? "",
			Name = r.Name?.Trim() ?? "",
			Description = r.Description?.Trim() ?? "",
		};
	}

	private static WeaponGroupDefinition MapWeaponGroup(WeaponGroupResource? r)
	{
		if (r == null)
			return new WeaponGroupDefinition();
		return new WeaponGroupDefinition
		{
			Id = r.Id?.Trim() ?? "",
			Name = r.Name?.Trim() ?? "",
			Description = r.Description?.Trim() ?? "",
		};
	}

	private static WeaponSubGroupDefinition MapWeaponSubGroup(WeaponSubGroupResource? r)
	{
		if (r == null)
			return new WeaponSubGroupDefinition();
		return new WeaponSubGroupDefinition
		{
			Id = r.Id?.Trim() ?? "",
			Name = r.Name?.Trim() ?? "",
			Description = r.Description?.Trim() ?? "",
		};
	}

	private static PotionDefinition ToPotionDefinition(PotionResource resource)
	{
		var baseDef = ToBaseItemDefinition(resource);

		return new PotionDefinition
		{
			Id = baseDef.Id,
			Name = baseDef.Name,
			Description = baseDef.Description,
			Rarity = baseDef.Rarity,
			ValueInCopper = baseDef.ValueInCopper,
			MaxStackSize = baseDef.MaxStackSize,
			CanDrop = baseDef.CanDrop,
			CanSell = baseDef.CanSell,
			ConsumedOnUse = resource.ConsumedOnUse,
			Effects = new List<ItemEffectDefinition>(baseDef.Effects),
		};
	}

	private static RestoreHealthEffectDefinition ToRestoreHealthEffectDefinition(RestoreHealthEffectResource resource)
	{
		var healDice = new DiceExpression
		{
			NumberOfDice = Math.Max(0, resource.NumberOfDice),
			DieType = resource.NumberOfDice > 0 ? resource.DiceType : DieType.d6,
			InD20CheckPool = false
		};

		return new RestoreHealthEffectDefinition
		{
			HealDice = healDice,
			FlatHealAmount = resource.FlatHealAmount
		};
	}

	private static DamageReductionEffectDefinition ToDamageReductionEffectDefinition(DamageReductionEffectResource resource)
	{
		DamageTypeDefinition? damageType = null;
		if (resource.DamageType != null)
		{
			var mapped = DamageTypeMapper.ToDomain(resource.DamageType);
			if (!string.IsNullOrWhiteSpace(mapped.Id))
				damageType = mapped;
		}
		return new DamageReductionEffectDefinition
		{
			// Selection rule: DamageType overrides DamageFamily; if both are unset, effect applies to all damage.
			DamageType = damageType,
			DamageFamily = damageType == null ? resource.DamageFamily : null,
			ReductionAmount = resource.ReductionAmount,
		};
	}
}
