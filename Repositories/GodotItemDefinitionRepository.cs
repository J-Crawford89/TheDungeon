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
				GD.PushWarning($"GodotItemDefinitionRepository: duplicate item id '{group.Key}'. Using last occurrence.");

			_all = mapped.GroupBy(i => i.Id).Select(g => g.Last()).ToList();
		}
		else
		{
			GD.PushWarning("GodotItemDefinitionRepository: ItemDatabase missing or empty; repository has no item definitions.");
			_all = [];
		}

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
