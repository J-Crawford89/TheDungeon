using System.Collections.Generic;

public interface IMonsterDefinitionRepository
{
	IReadOnlyList<MonsterDefinition> All { get; }
}
