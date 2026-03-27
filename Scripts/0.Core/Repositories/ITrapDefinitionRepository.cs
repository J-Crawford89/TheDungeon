using System.Collections.Generic;

public interface ITrapDefinitionRepository
{
	IReadOnlyList<TrapDefinition> All { get; }
}
