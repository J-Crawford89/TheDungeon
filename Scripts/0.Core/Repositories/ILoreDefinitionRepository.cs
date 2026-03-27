using System.Collections.Generic;

public interface ILoreDefinitionRepository
{
	IReadOnlyList<LoreDefinition> All { get; }
}
