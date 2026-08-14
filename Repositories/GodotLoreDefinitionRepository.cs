#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotLoreDefinitionRepository : ILoreDefinitionRepository
{
	private readonly IReadOnlyList<LoreDefinition> _all;

	public GodotLoreDefinitionRepository(LoreResourceDatabase? database)
	{
		if (database?.LoreEntries == null || database.LoreEntries.Count == 0)
		{
			GD.PushWarning("GodotLoreDefinitionRepository: LoreDatabase missing or empty.");
			_all = [];
			return;
		}

		var mapped = database.LoreEntries
			.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
			.Select(LoreMapper.ToDomain)
			.ToList();

		foreach (var group in mapped.GroupBy(static l => l.Id).Where(static g => g.Count() > 1))
			GD.PushWarning($"GodotLoreDefinitionRepository: duplicate lore id '{group.Key}'. Using last occurrence.");

		_all = mapped.GroupBy(static l => l.Id).Select(static g => g.Last()).ToList();
	}

	public IReadOnlyList<LoreDefinition> All => _all;
}
