/// <summary>A quantity of an inventory item referenced by <see cref="ItemDefinition.Id"/>.
/// Extend this type for future nested rules (curse, trapped loot, etc.).</summary>
public sealed class LootableItemDefinition
{
	public string ItemDefinitionId { get; set; } = string.Empty;

	public int Quantity { get; set; }

	/// <summary>Optional harvest check per unit taken; <c>null</c> or <see cref="HarvestRequirement.HarvestDc"/> ≤ 0 means no roll.</summary>
	public HarvestRequirement? Harvest { get; set; }
}
