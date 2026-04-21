#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotItemDefinitionRepository : IItemDefinitionRepository
{
	private readonly IReadOnlyList<ItemDefinition> _all;
	private readonly Dictionary<string, ItemDefinition> _byId;

	public GodotItemDefinitionRepository(ItemResourceDatabase? database)
	{
		if (database?.Items != null && database.Items.Count > 0)
		{
			var mapped = database.Items
				.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
				.Select(ItemMapper.ToDomain)
				.ToList();

			foreach (var group in mapped.GroupBy(i => i.Id).Where(g => g.Count() > 1))
				Godot.GD.PushWarning($"GodotItemDefinitionRepository: duplicate item id '{group.Key}'. Using last occurrence.");

			_all = mapped.GroupBy(i => i.Id).Select(g => g.Last()).ToList();
		}
		else
		{
			Godot.GD.PushWarning("GodotItemDefinitionRepository: ItemDatabase missing or empty; using DefaultAll(). Assign ItemDatabase on GameRoot.");
			_all = DefaultAll();
		}

		_byId = _all.ToDictionary(static i => i.Id, static i => i);
	}

	public IReadOnlyList<ItemDefinition> All => _all;

	public ItemDefinition? TryGetById(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		var key = id.Trim();
		if (_byId.TryGetValue(key, out var def))
			return def;
		Godot.GD.PushWarning($"GodotItemDefinitionRepository: unknown item id '{key}'.");
		return null;
	}

	public IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition =>
		_all.OfType<T>().ToArray();

	private static IReadOnlyList<ItemDefinition> DefaultAll() =>
		new[]
		{
			new PotionDefinition
			{
				Id = InventoryIds.HealthPotionItemId,
				Name = "Health potion",
				Description = "Restores health.",
				Rarity = ItemRarity.Common,
				ValueInGold = 0,
				MaxStackSize = 99,
				CanDrop = true,
				CanSell = true,
				ConsumedOnUse = true,
				Effects = new List<ItemEffectDefinition>
				{
					new RestoreHealthEffectDefinition
					{
						HealDice = new DiceExpression { NumberOfDice = 0, DieType = DieType.d6 },
						FlatHealAmount = 4
					}
				}
			}
		};
}
