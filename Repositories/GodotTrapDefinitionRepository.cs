#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotTrapDefinitionRepository : ITrapDefinitionRepository
{
	private readonly IReadOnlyList<TrapDefinition> _all;

	public GodotTrapDefinitionRepository(TrapResourceDatabase? database)
	{
		if (database?.Traps == null || database.Traps.Count == 0)
		{
			GD.PushWarning("GodotTrapDefinitionRepository: TrapDatabase missing or empty.");
			_all = [];
			return;
		}

		var mapped = database.Traps
			.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
			.Select(TrapMapper.ToDomain)
			.ToList();

		foreach (var group in mapped.GroupBy(static t => t.Id).Where(static g => g.Count() > 1))
			GD.PushWarning($"GodotTrapDefinitionRepository: duplicate trap id '{group.Key}'. Using last occurrence.");

		_all = mapped.GroupBy(static t => t.Id).Select(static g => g.Last()).ToList();
	}

	public IReadOnlyList<TrapDefinition> All => _all;
}
