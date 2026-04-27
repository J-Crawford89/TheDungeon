#nullable enable
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>Spawns a <see cref="CorpseFeature"/> on monster death; death-loot row validation mirrors trap disarm salvage.</summary>
public static class CombatCorpseHelper
{
	/// <summary>Always appends a corpse, even when <see cref="Contents"/> end up empty after validation.</summary>
	public static void SpawnCorpseOnMonsterDeath(
		GameSessionState session,
		DungeonRoom room,
		MonsterDefinition definition,
		IItemDefinitionRepository items,
		NarrativeService narrative)
	{
		var corpse = new CorpseFeature { HarvestDc = definition.HarvestDc };
		var validRows = new List<LootableItemDefinition>();

		if (definition.DeathLoot is { Count: > 0 })
		{
			foreach (var row in definition.DeathLoot)
			{
				var id = row.ItemDefinitionId?.Trim() ?? "";
				var qty = row.Quantity < 0 ? 0 : row.Quantity;
				if (qty <= 0 || string.IsNullOrWhiteSpace(id))
					continue;

				if (items.TryGetById(id) == null)
				{
					session.AppendGameLog(narrative.ForTrapDisarmGrantItemMissing(id));
					Trace.TraceWarning(
						"CombatCorpseHelper: monster '{0}' ({1}) death loot references unknown item id '{2}'.",
						definition.Id,
						definition.Name,
						id);
					continue;
				}

				validRows.Add(new LootableItemDefinition { ItemDefinitionId = id, Quantity = qty });
			}
		}

		corpse.Contents.AddRange(validRows);
		room.Features.Add(corpse);
	}
}
