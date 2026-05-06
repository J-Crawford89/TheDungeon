#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotItemDefinitionRepository : IItemDefinitionRepository
{
	private readonly IReadOnlyList<ItemDefinition> _all;
	private readonly Dictionary<string, ItemDefinition> _byId;

	/// <summary>
	/// Loads items from <paramref name="database"/>; then adds any item ids referenced only on
	/// <see cref="MonsterResource.DeathLoot"/> rows (embedded <see cref="LootableItemResource.ItemResource"/>)
	/// so corpse/salvage validation can resolve definitions without duplicating every loot item in the database.
	/// </summary>
	public GodotItemDefinitionRepository(ItemResourceDatabase? database, MonsterResourceDatabase? monsterDatabase = null)
	{
		List<ItemDefinition> merged;

		if (database?.Items != null && database.Items.Count > 0)
		{
			var mapped = database.Items
				.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
				.Select(ItemMapper.ToDomain)
				.ToList();

			foreach (var group in mapped.GroupBy(i => i.Id).Where(g => g.Count() > 1))
				GD.PushWarning($"GodotItemDefinitionRepository: duplicate item id '{group.Key}'. Using last occurrence.");

			merged = mapped.GroupBy(i => i.Id).Select(g => g.Last()).ToList();
		}
		else
		{
			GD.PushWarning(
				"GodotItemDefinitionRepository: ItemDatabase missing or empty; definitions may come only from monster death-loot ItemResources.");
			merged = [];
		}

		var byId = merged.ToDictionary(static i => i.Id, static i => i);

		if (monsterDatabase?.Monsters != null)
		{
			foreach (var monster in monsterDatabase.Monsters)
			{
				if (monster?.DeathLoot == null || monster.DeathLoot.Count == 0)
					continue;
				foreach (var row in monster.DeathLoot)
				{
					if (row?.ItemResource == null)
						continue;
					var id = row.ItemResource.Id?.Trim() ?? "";
					if (string.IsNullOrWhiteSpace(id) || byId.ContainsKey(id))
						continue;
					var def = ItemMapper.ToDomain(row.ItemResource);
					merged.Add(def);
					byId[id] = def;
				}
			}
		}

		if (merged.Count == 0)
		{
			GD.PushWarning(
				"GodotItemDefinitionRepository: no item definitions (ItemDatabase empty and no usable monster death-loot ItemResources).");
			_all = [];
			_byId = [];
			return;
		}

		foreach (var group in merged.GroupBy(i => i.Id).Where(g => g.Count() > 1))
			GD.PushWarning($"GodotItemDefinitionRepository: duplicate item id '{group.Key}'. Using last occurrence.");

		_all = merged.GroupBy(i => i.Id).Select(g => g.Last()).ToList();
		_byId = _all.ToDictionary(static i => i.Id, static i => i);
	}

	public IReadOnlyList<ItemDefinition> All => _all;

	public ItemDefinition? TryGetById(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		return _byId.TryGetValue(key, out var def) ? def : null;
	}

	public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
		_all.OfType<T>().ToArray();

}
