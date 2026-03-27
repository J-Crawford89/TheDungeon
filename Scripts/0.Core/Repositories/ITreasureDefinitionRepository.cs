using System.Collections.Generic;

public interface ITreasureDefinitionRepository
{
	IReadOnlyList<TreasureDefinition> All { get; }
}
