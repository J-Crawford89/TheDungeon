#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotCharacterClassDefinitionRepository : ICharacterClassDefinitionRepository
{
	private readonly IReadOnlyList<CharacterClassDefinition> _all;

	public GodotCharacterClassDefinitionRepository(CharacterClassResourceDatabase? database)
	{
		if (database?.CharacterClasses != null && database.CharacterClasses.Count > 0)
			_all = database.CharacterClasses.Select(CharacterClassMapper.ToDomain).ToList();
		else
			_all = [];
	}

	public IReadOnlyList<CharacterClassDefinition> All => _all;
}
