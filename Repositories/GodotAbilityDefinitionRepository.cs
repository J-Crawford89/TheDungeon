#nullable enable
using Godot;
using System.Collections.Generic;
using System.Linq;

public sealed class GodotAbilityDefinitionRepository : IAbilityDefinitionRepository
{
	private readonly IReadOnlyList<AbilityDefinition> _all;
	private readonly Dictionary<string, AbilityDefinition> _byId;

	public GodotAbilityDefinitionRepository(AbilityResourceDatabase? database)
	{
		if (database?.Abilities != null && database.Abilities.Count > 0)
		{
			var mapped = database.Abilities
				.Where(a => a != null && !string.IsNullOrWhiteSpace(a.Id))
				.Select(AbilityMapper.ToDomain)
				.ToList();
			foreach (var group in mapped.GroupBy(a => a.Id).Where(g => g.Count() > 1))
				GD.PushWarning($"GodotAbilityDefinitionRepository: duplicate ability id '{group.Key}'. Using last occurrence.");

			_all = mapped.GroupBy(a => a.Id).Select(g => g.Last()).ToList();
		}
		else
			_all = [];

		_byId = _all.ToDictionary(a => a.Id, a => a);
	}

	public IReadOnlyList<AbilityDefinition> All => _all;

	public AbilityDefinition? TryGetById(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		return _byId.TryGetValue(id.Trim(), out var def) ? def : null;
	}
}
