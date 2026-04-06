#nullable enable

public interface IAbilityDefinitionRepository
{
	IReadOnlyList<AbilityDefinition> All { get; }
	AbilityDefinition? TryGetById(string id);
}
