#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotNpcDefinitionRepository : INpcDefinitionRepository
{
	private readonly IReadOnlyList<NpcDefinition> _all;

	public GodotNpcDefinitionRepository(NpcResourceDatabase? database)
	{
		if (database?.Npcs == null || database.Npcs.Count == 0)
		{
			GD.PushWarning("GodotNpcDefinitionRepository: NpcDatabase missing or empty.");
			_all = [];
			return;
		}

		var mapped = database.Npcs
			.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
			.Select(NpcMapper.ToDomain)
			.ToList();

		foreach (var group in mapped.GroupBy(static n => n.Id).Where(static g => g.Count() > 1))
			GD.PushWarning($"GodotNpcDefinitionRepository: duplicate npc id '{group.Key}'. Using last occurrence.");

		_all = mapped.GroupBy(static n => n.Id).Select(static g => g.Last()).ToList();
	}

	public IReadOnlyList<NpcDefinition> All => _all;
}
