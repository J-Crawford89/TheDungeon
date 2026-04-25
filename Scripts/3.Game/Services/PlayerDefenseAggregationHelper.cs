#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

public static class PlayerDefenseAggregationHelper
{
	public static void RecomputeFromEquippedArmor(PlayerState player)
	{
		ArgumentNullException.ThrowIfNull(player);

		player.TotalArmorBonus = 0;
		player.TotalDamageReduction = 0;
		player.TotalAgilityPenalty = 0;
		player.DamageReductionByDamageTypeId.Clear();
		player.DamageReductionByDamageFamily.Clear();
		player.DamageReductionAllDamage = 0;

		var equippedBySlot = player.InventoryState.EquippedBySlot;
		var seen = new HashSet<Guid>();
		foreach (var entry in equippedBySlot)
		{
			var equipped = entry.Value;
			if (equipped == null || !seen.Add(equipped.InstanceId))
				continue;
			if (equipped.Definition is not ArmorDefinition armor)
				continue;

			player.TotalArmorBonus += armor.ArmorBonus;
			player.TotalAgilityPenalty += armor.AgilityPenalty;

			AccumulateReductionEffects(player, armor.Effects.OfType<DamageReductionEffectDefinition>());
		}
	}

	private static void AccumulateReductionEffects(PlayerState player, IEnumerable<DamageReductionEffectDefinition> effects)
	{
		foreach (var effect in effects)
		{
			if (effect == null || effect.ReductionAmount == 0)
				continue;

			var applied = false;
			if (effect.DamageType is { } type)
			{
				var id = type.Id?.Trim() ?? "";
				if (id.Length > 0)
				{
					Add(player.DamageReductionByDamageTypeId, id, effect.ReductionAmount);
					applied = true;
				}
			}
			else if (effect.DamageFamily is { } family)
			{
				Add(player.DamageReductionByDamageFamily, family, effect.ReductionAmount);
				applied = true;
			}
			else
			{
				player.DamageReductionAllDamage += effect.ReductionAmount;
				applied = true;
			}

			if (applied)
				player.TotalDamageReduction += effect.ReductionAmount;
		}
	}

	private static void Add<TKey>(Dictionary<TKey, int> map, TKey key, int value) where TKey : notnull
	{
		if (map.TryGetValue(key, out var existing))
			map[key] = existing + value;
		else
			map[key] = value;
	}
}
