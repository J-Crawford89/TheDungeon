#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotDamageTypeDefinitionRepository : IDamageTypeDefinitionRepository
{
	private readonly IReadOnlyList<DamageTypeDefinition> _all;
	private readonly Dictionary<string, DamageTypeDefinition> _byId;

	public GodotDamageTypeDefinitionRepository(DamageTypeResourceDatabase? database)
	{
		if (database?.DamageTypes != null && database.DamageTypes.Count > 0)
		{
			var mapped = database.DamageTypes
				.Where(static r => r != null && !string.IsNullOrWhiteSpace(r.Id))
				.Select(DamageTypeMapper.ToDomain)
				.Where(static d => !string.IsNullOrWhiteSpace(d.Id))
				.ToList();

			foreach (var group in mapped.GroupBy(d => d.Id).Where(g => g.Count() > 1))
				Godot.GD.PushWarning($"GodotDamageTypeDefinitionRepository: duplicate damage type id '{group.Key}'. Using last occurrence.");

			_all = mapped.GroupBy(d => d.Id).Select(g => g.Last()).ToList();
		}
		else
			_all = [];

		_byId = _all.ToDictionary(static d => d.Id, static d => d);
	}

	public IReadOnlyList<DamageTypeDefinition> All => _all;

	public DamageTypeDefinition? TryGetById(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;
		return _byId.TryGetValue(id.Trim(), out var def) ? def : null;
	}
}
