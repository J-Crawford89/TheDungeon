#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotCharacterRaceDefinitionRepository : ICharacterRaceDefinitionRepository
{
	private readonly IReadOnlyList<CharacterRaceDefinition> _all;

	public GodotCharacterRaceDefinitionRepository(CharacterRaceResourceDatabase? database)
	{
		if (database?.CharacterRaces != null && database.CharacterRaces.Count > 0)
			_all = database.CharacterRaces.Select(CharacterRaceMapper.ToDomain).ToList();
		else
			_all = [];
	}

	public IReadOnlyList<CharacterRaceDefinition> All => _all;
}
