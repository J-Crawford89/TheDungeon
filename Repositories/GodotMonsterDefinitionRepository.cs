#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotMonsterDefinitionRepository : IMonsterDefinitionRepository
{
	private readonly IReadOnlyList<MonsterDefinition> _all;

	public GodotMonsterDefinitionRepository(MonsterResourceDatabase? database)
	{
		if (database?.Monsters == null || database.Monsters.Count == 0)
		{
			GD.PushWarning("GodotMonsterDefinitionRepository: MonsterDatabase missing or empty.");
			_all = [];
			return;
		}

		var mapped = database.Monsters
			.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
			.Select(MonsterMapper.ToDomain)
			.ToList();

		foreach (var group in mapped.GroupBy(static m => m.Id).Where(static g => g.Count() > 1))
			GD.PushWarning($"GodotMonsterDefinitionRepository: duplicate monster id '{group.Key}'. Using last occurrence.");

		_all = mapped.GroupBy(static m => m.Id).Select(static g => g.Last()).ToList();
	}

	public IReadOnlyList<MonsterDefinition> All => _all;
}
