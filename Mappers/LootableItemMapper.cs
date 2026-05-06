#nullable enable
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public static class LootableItemMapper
{
	public static LootableItemDefinition ToDomain(LootableItemResource resource)
	{
		var id = resource.ItemResource?.Id?.Trim() ?? "";
		var qty = resource.Quantity < 0 ? 0 : resource.Quantity;
		HarvestRequirement? harvest = null;
		if (resource.Harvest is { HarvestDc: > 0 } hr)
		{
			harvest = new HarvestRequirement
			{
				HarvestDc = hr.HarvestDc,
				HarvestAbility = hr.HarvestAbility,
			};
		}

		return new LootableItemDefinition { ItemDefinitionId = id, Quantity = qty, Harvest = harvest };
	}

	public static List<LootableItemDefinition> ToDomainList(Array<LootableItemResource>? loot)
	{
		if (loot == null || loot.Count == 0)
			return [];
		var list = new List<LootableItemDefinition>();
		foreach (var entry in loot.OfType<LootableItemResource>())
		{
			var row = ToDomain(entry);
			if (row.Quantity <= 0 || string.IsNullOrWhiteSpace(row.ItemDefinitionId))
				continue;
			list.Add(row);
		}

		return list;
	}
}
