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
			ValueInGold = resource.ValueInGold,
			MaxStackSize = resource.MaxStackSize,
			CanDrop = resource.CanDrop,
			CanSell = resource.CanSell
		};

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
			ValueInGold = b.ValueInGold,
			MaxStackSize = b.MaxStackSize,
			CanDrop = b.CanDrop,
			CanSell = b.CanSell,
			Slots = MapEquipmentSlots(resource.Slots),
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
			ValueInGold = eq.ValueInGold,
			MaxStackSize = eq.MaxStackSize,
			CanDrop = eq.CanDrop,
			CanSell = eq.CanSell,
			Slots = eq.Slots,
			ArmorBonus = resource.ArmorBonus,
		};
	}

	private static WeaponDefinition ToWeaponDefinition(WeaponResource resource)
	{
		var eq = ToEquipmentDefinition(resource);
		var damage = new List<DamageComponent>();
		if (resource.DamageComponents != null)
		{
			foreach (var part in resource.DamageComponents)
			{
				if (part == null)
					continue;
				if (part.DamageType == null)
				{
					GD.PushWarning($"ItemMapper: weapon '{resource.Id}' has a damage component with no DamageType; skipped.");
					continue;
				}

				var typeId = part.DamageType.Id?.Trim() ?? "";
				if (typeId.Length == 0)
				{
					GD.PushWarning($"ItemMapper: weapon '{resource.Id}' has a damage component with empty damage type id; skipped.");
					continue;
				}

				var dice = new DiceExpression
				{
					NumberOfDice = Math.Max(0, part.NumberOfDice),
					DieType = part.NumberOfDice > 0 ? part.DiceType : DieType.d6,
					InD20CheckPool = part.InD20CheckPool,
				};
				damage.Add(new DamageComponent(dice, part.FlatAmount, DamageTypeMapper.ToDomain(part.DamageType)));
			}
		}

		return new WeaponDefinition
		{
			Id = eq.Id,
			Name = eq.Name,
			Description = eq.Description,
			Rarity = eq.Rarity,
			ValueInGold = eq.ValueInGold,
			MaxStackSize = eq.MaxStackSize,
			CanDrop = eq.CanDrop,
			CanSell = eq.CanSell,
			Slots = eq.Slots,
			DamageComponents = damage,
		};
	}

	private static PotionDefinition ToPotionDefinition(PotionResource resource)
	{
		var baseDef = ToBaseItemDefinition(resource);
		var effects = new List<ItemEffectDefinition>();
		if (resource.RestoreHealthEffects != null)
		{
			foreach (var restore in resource.RestoreHealthEffects)
			{
				if (restore == null)
					continue;
				effects.Add(ToRestoreHealthEffectDefinition(restore));
			}
		}

		return new PotionDefinition
		{
			Id = baseDef.Id,
			Name = baseDef.Name,
			Description = baseDef.Description,
			Rarity = baseDef.Rarity,
			ValueInGold = baseDef.ValueInGold,
			MaxStackSize = baseDef.MaxStackSize,
			CanDrop = baseDef.CanDrop,
			CanSell = baseDef.CanSell,
			ConsumedOnUse = resource.ConsumedOnUse,
			Effects = effects
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
}
