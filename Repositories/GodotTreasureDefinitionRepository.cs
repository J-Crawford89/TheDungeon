#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotTreasureDefinitionRepository : ITreasureDefinitionRepository
{
	private readonly IReadOnlyList<TreasureDefinition> _all;

	public GodotTreasureDefinitionRepository(TreasureResourceDatabase? database)
	{
		if (database?.Treasures == null || database.Treasures.Count == 0)
		{
			GD.PushWarning("GodotTreasureDefinitionRepository: TreasureDatabase missing or empty.");
			_all = [];
			return;
		}

		var mapped = database.Treasures
			.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
			.Select(TreasureMapper.ToDomain)
			.ToList();

		foreach (var group in mapped.GroupBy(static t => t.Id).Where(static g => g.Count() > 1))
			GD.PushWarning($"GodotTreasureDefinitionRepository: duplicate treasure id '{group.Key}'. Using last occurrence.");

		_all = mapped.GroupBy(static t => t.Id).Select(static g => g.Last()).ToList();
	}

	public IReadOnlyList<TreasureDefinition> All => _all;
}
