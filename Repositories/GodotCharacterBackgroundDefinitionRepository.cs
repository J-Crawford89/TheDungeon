#nullable enable
using System.Collections.Generic;
using System.Linq;

public sealed class GodotCharacterBackgroundDefinitionRepository : ICharacterBackgroundDefinitionRepository
{
	private readonly IReadOnlyList<CharacterBackgroundDefinition> _all;

	public GodotCharacterBackgroundDefinitionRepository(CharacterBackgroundResourceDatabase? database)
	{
		if (database?.CharacterBackgrounds != null && database.CharacterBackgrounds.Count > 0)
			_all = database.CharacterBackgrounds.Select(CharacterBackgroundMapper.ToDomain).ToList();
		else
			_all = [];
	}

	public IReadOnlyList<CharacterBackgroundDefinition> All => _all;
}
