#nullable enable

using System.Collections.Generic;

public interface IItemDefinitionRepository
{
	IReadOnlyList<ItemDefinition> All { get; }

	ItemDefinition? TryGetById(string id);

	/// <summary>All definitions whose runtime type is assignable to <typeparamref name="T"/> (e.g. all <see cref="PotionDefinition"/>).</summary>
	IReadOnlyList<T> GetDefinitionsOfType<T>() where T : ItemDefinition;
}
