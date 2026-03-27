using System.Collections.Generic;

public interface INpcDefinitionRepository
{
	IReadOnlyList<NpcDefinition> All { get; }
}
