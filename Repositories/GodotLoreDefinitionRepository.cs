#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotLoreDefinitionRepository : ILoreDefinitionRepository
{
	private readonly IReadOnlyList<LoreDefinition> _all;

	public GodotLoreDefinitionRepository(LoreResourceDatabase? database)
	{
		if (database?.LoreEntries != null && database.LoreEntries.Count > 0)
			_all = database.LoreEntries.Select(LoreMapper.ToDomain).ToList();
		else
			_all = DefaultAll();
	}

	public IReadOnlyList<LoreDefinition> All => _all;

	private static IReadOnlyList<LoreDefinition> DefaultAll() =>
		new[]
		{
			new LoreDefinition
			{
				Id = "cracked_plaque",
				Name = "Cracked stone plaque",
				Description = "Weathered runes you can barely read."
			}
		};
}
