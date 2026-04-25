#nullable enable

public interface IDamageTypeDefinitionRepository
{
	IReadOnlyList<DamageTypeDefinition> All { get; }

	DamageTypeDefinition? TryGetById(string id);
}
