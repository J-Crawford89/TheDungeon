public sealed partial class NarrativeService
{
	public string ForContainerLootTransferStart(string containerKindLabel, int distinctStacks) =>
		distinctStacks <= 0
			? $"You search the {containerKindLabel}, but find nothing to take."
			: $"You search the {containerKindLabel} ({distinctStacks} stack(s)).";

	public string ForLootTakenFromContainer(string containerKindLabel, string itemDisplayName, int quantity) =>
		quantity <= 1
			? $"From the {containerKindLabel}, you take {itemDisplayName}."
			: $"From the {containerKindLabel}, you take {itemDisplayName} ×{quantity}.";

	public string ForLootDefinitionMissing(string itemDefinitionId) =>
		string.IsNullOrWhiteSpace(itemDefinitionId)
			? "Something here cannot be identified — you leave it."
			: $"You find an item listed as '{itemDefinitionId.Trim()}', but it no longer exists in your catalog — you leave it.";
}
