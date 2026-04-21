using System;
using System.Collections.Generic;

public static class ItemMapper
{
	public static ItemDefinition ToDomain(ItemResource resource) =>
		resource switch
		{
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
