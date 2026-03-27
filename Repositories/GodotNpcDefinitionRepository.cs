#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotNpcDefinitionRepository : INpcDefinitionRepository
{
	private readonly IReadOnlyList<NpcDefinition> _all;

	public GodotNpcDefinitionRepository(NpcResourceDatabase? database)
	{
		if (database?.Npcs != null && database.Npcs.Count > 0)
			_all = database.Npcs.Select(NpcMapper.ToDomain).ToList();
		else
			_all = DefaultAll();
	}

	public IReadOnlyList<NpcDefinition> All => _all;

	private static IReadOnlyList<NpcDefinition> DefaultAll() =>
		new[]
		{
			new NpcDefinition
			{
				Id = "wounded_traveler",
				Name = "A wounded traveler"
			}
		};
}
